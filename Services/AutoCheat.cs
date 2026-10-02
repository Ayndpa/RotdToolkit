using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RotdToolkit.Services
{
    /// <summary>
    /// Pure in-memory auto-locator for Road of the Dead 1 (no game file edits).
    ///
    /// Anchors (re-found on every attach, so ASLR/heap layout does not matter):
    ///  * RDPlayerInfo via its default key-binding block (ten consecutive int32:
    ///    65,68,87,83,32,69,82,70,81,80).  Relative to the first key (anchor S):
    ///        S-60 m_iRP           S-56 m_bIsAwareOfStoryMode
    ///        S-52 m_bStoryModeFinished  S-48 m_iStoryLevel
    ///        S-44 m_iStoryHardcoreLevel S-40 m_iBestHitCombo  S-36 m_iBestFlipFlop
    ///        S-32..S-4  the eight upgrade levels (int, 0..3)
    ///    (verified against the running English: RP read back 606.)
    ///  * RDPlayer via the adjacent Number pair (m_fMaxSpeed,m_fXMaxSpeed); one
    ///    instance exists per gameplay mode, so several copies are expected.
    /// </summary>
    public sealed class AutoCheat
    {
        private readonly MemoryTrainer _mem;

        public long PlayerInfoAnchor { get; private set; } = -1;
        public List<long> PlayerSpeedAddresses { get; private set; } = new List<long>();

        private static readonly int[] Keyblock = { 65, 68, 87, 83, 32, 69, 82, 70, 81, 80 };

        private static readonly double[] MaxSpeeds = { 30.555555555555557, 31.944444444444443, 33.333333333333336, 34.72222222222222 };
        private static readonly double[] XMaxSpeeds = { 4.0, 5.33, 6.66, 8.0 };

        // Offsets relative to the m_fMaxSpeed double inside an RDPlayer copy.
        private static readonly long[] HpOffsets = { 0x68, 0x80, 0x88 };

        public AutoCheat(MemoryTrainer mem)
        {
            _mem = mem;
        }

        public bool HasPlayerInfo => PlayerInfoAnchor > 0;
        public bool HasPlayers => PlayerSpeedAddresses.Count > 0;

        public long RpAddress => PlayerInfoAnchor - 60;
        public long[] LevelAddresses => new[] { PlayerInfoAnchor - 32, -28, -24, -20, -16, -12, -8, -4 };
        public long AwareAddress => PlayerInfoAnchor - 56;
        public long FinishedAddress => PlayerInfoAnchor - 52;
        public long StoryLevelAddress => PlayerInfoAnchor - 48;
        public long StoryHardLevelAddress => PlayerInfoAnchor - 44;

        // ---------------- locate ----------------

        public bool LocatePlayerInfo()
        {
            PlayerInfoAnchor = -1;
            var pattern = new byte[Keyblock.Length * 4];
            for (int i = 0; i < Keyblock.Length; i++)
            {
                Array.Copy(BitConverter.GetBytes(Keyblock[i]), 0, pattern, i * 4, 4);
            }

            var hits = _mem.FindBytes(pattern, 8, CancellationToken.None);
            if (hits.Count == 0)
            {
                return false;
            }

            PlayerInfoAnchor = hits[0].Address;
            return true;
        }

        public bool LocatePlayers()
        {
            PlayerSpeedAddresses = new List<long>();
            // One pass per MaxSpeeds value (4 passes instead of 16 pair patterns);
            // a real player also has m_fXMaxSpeed right after it, so require that.
            foreach (var e in MaxSpeeds)
            {
                var p = BitConverter.GetBytes(e);
                foreach (var hit in _mem.FindBytes(p, 64, CancellationToken.None))
                {
                    var next = _mem.ReadBytes(hit.Address + 8, 8);
                    if (next == null)
                    {
                        continue;
                    }
                    double x = BitConverter.ToDouble(next, 0);
                    if (XMaxSpeeds.Any(v => Math.Abs(v - x) < 1e-6))
                    {
                        PlayerSpeedAddresses.Add(hit.Address);
                    }
                }
            }
            PlayerSpeedAddresses = PlayerSpeedAddresses.Distinct().OrderBy(a => a).ToList();
            return PlayerSpeedAddresses.Count > 0;
        }

        // ---------------- RDPlayerInfo cheats ----------------

        private int _rpOriginal;
        private int[] _levelOriginal;
        private int[] _unlockOriginal;

        public bool ApplyMoney(bool on)
        {
            if (!HasPlayerInfo) return false;
            if (on)
            {
                _rpOriginal = ReadInt(RpAddress);
                _mem.WriteBytes(RpAddress, MemoryTrainer.ToBytes(ScanType.Int32, 999999));
                _mem.SetFrozen(RpAddress, ScanType.Int32, 999999, true);
            }
            else
            {
                _mem.SetFrozen(RpAddress, ScanType.Int32, 0, false);
                _mem.WriteBytes(RpAddress, MemoryTrainer.ToBytes(ScanType.Int32, _rpOriginal));
            }
            return true;
        }

        public bool ApplyMaxUpgrade(bool on)
        {
            if (!HasPlayerInfo) return false;
            var addrs = LevelAddresses;
            if (on)
            {
                _levelOriginal = new int[addrs.Length];
                for (int i = 0; i < addrs.Length; i++)
                {
                    _levelOriginal[i] = ReadInt(addrs[i]);
                    int cur = _levelOriginal[i];
                    if (cur >= 0 && cur <= 3)   // skip any non-level slot (e.g. an array pointer)
                    {
                        _mem.WriteBytes(addrs[i], MemoryTrainer.ToBytes(ScanType.Int32, 3));
                    }
                }
            }
            else if (_levelOriginal != null)
            {
                for (int i = 0; i < addrs.Length; i++)
                {
                    _mem.WriteBytes(addrs[i], MemoryTrainer.ToBytes(ScanType.Int32, _levelOriginal[i]));
                }
            }
            return true;
        }

        public bool ApplyUnlock(bool on)
        {
            if (!HasPlayerInfo) return false;
            var addrs = new[] { AwareAddress, FinishedAddress, StoryLevelAddress, StoryHardLevelAddress };
            if (on)
            {
                _unlockOriginal = new[] { ReadInt(addrs[0]), ReadInt(addrs[1]), ReadInt(addrs[2]), ReadInt(addrs[3]) };
                _mem.WriteBytes(addrs[0], MemoryTrainer.ToBytes(ScanType.Int32, 1));
                _mem.WriteBytes(addrs[1], MemoryTrainer.ToBytes(ScanType.Int32, 1));
                _mem.WriteBytes(addrs[2], MemoryTrainer.ToBytes(ScanType.Int32, 18));
                _mem.WriteBytes(addrs[3], MemoryTrainer.ToBytes(ScanType.Int32, 18));
            }
            else if (_unlockOriginal != null)
            {
                for (int i = 0; i < addrs.Length; i++)
                {
                    _mem.WriteBytes(addrs[i], MemoryTrainer.ToBytes(ScanType.Int32, _unlockOriginal[i]));
                }
            }
            return true;
        }

        // ---------------- RDPlayer cheats ----------------

        public bool ApplySpeed(bool on, double maxSpeed = 80.0, double xMaxSpeed = 24.0)
        {
            if (!HasPlayers) return false;
            foreach (var addr in PlayerSpeedAddresses)
            {
                if (on)
                {
                    _mem.SetFrozen(addr, ScanType.Double, maxSpeed, true);
                    _mem.SetFrozen(addr + 8, ScanType.Double, xMaxSpeed, true);
                }
                else
                {
                    _mem.SetFrozen(addr, ScanType.Double, 0, false);
                    _mem.SetFrozen(addr + 8, ScanType.Double, 0, false);
                }
            }
            return true;
        }

        public bool ApplyGod(bool on)
        {
            if (!HasPlayers) return false;
            foreach (var addr in PlayerSpeedAddresses)
            {
                foreach (var off in HpOffsets)
                {
                    long hp = addr + off;
                    _mem.WriteBytes(hp, MemoryTrainer.ToBytes(ScanType.Double, 100.0));
                    _mem.SetFrozen(hp, ScanType.Double, 100.0, on);
                }
            }
            return true;
        }

        public void ClearAll()
        {
            ApplyMoney(false);
            ApplyMaxUpgrade(false);
            ApplyUnlock(false);
            ApplySpeed(false);
            if (HasPlayers)
            {
                foreach (var addr in PlayerSpeedAddresses)
                {
                    foreach (var off in HpOffsets)
                    {
                        _mem.SetFrozen(addr + off, ScanType.Double, 0, false);
                    }
                }
            }
        }

        private int ReadInt(long addr)
        {
            var b = _mem.ReadBytes(addr, 4);
            return b == null ? -1 : BitConverter.ToInt32(b, 0);
        }
    }
}
