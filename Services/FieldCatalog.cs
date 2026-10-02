using System.Collections.Generic;

namespace RotdToolkit.Services
{
    public sealed class FieldMeta
    {
        public string Label;
        public string Group;
        public bool Advanced;
    }

    /// <summary>中文名称与分组，未知字段回退为原始名称。</summary>
    public static class FieldCatalog
    {
        private static readonly Dictionary<string, FieldMeta> Map = new Dictionary<string, FieldMeta>
        {
            // 资源 / 进度
            ["m_iRP"] = new FieldMeta { Label = "金钱 (RP)", Group = "资源与进度" },
            ["m_iStoryLevel"] = new FieldMeta { Label = "剧情关卡", Group = "资源与进度" },
            ["m_iStoryHardcoreLevel"] = new FieldMeta { Label = "困难剧情关卡", Group = "资源与进度" },
            ["m_iStoryTutorialIndex"] = new FieldMeta { Label = "剧教学进度", Group = "资源与进度" },
            ["m_bStoryModeFinished"] = new FieldMeta { Label = "剧情已通关", Group = "资源与进度" },
            ["m_bIsAwareOfStoryMode"] = new FieldMeta { Label = "已解锁剧情模式", Group = "资源与进度" },
            ["m_bBeenInGarage"] = new FieldMeta { Label = "到过车库", Group = "资源与进度" },
            ["m_bSavedOnce"] = new FieldMeta { Label = "已保存过(内部)", Group = "资源与进度", Advanced = true },

            // 改装等级
            ["m_iPerceptionLevel"] = new FieldMeta { Label = "感知 等级", Group = "改装等级" },
            ["m_iBodyArmorLevel"] = new FieldMeta { Label = "车体装甲 等级", Group = "改装等级" },
            ["m_iFirearmLevel"] = new FieldMeta { Label = "枪械 等级", Group = "改装等级" },
            ["m_iWindshieldLevel"] = new FieldMeta { Label = "挡风玻璃 等级", Group = "改装等级" },
            ["m_iEngineLevel"] = new FieldMeta { Label = "引擎 等级", Group = "改装等级" },
            ["m_iBumperLevel"] = new FieldMeta { Label = "保险杠 等级", Group = "改装等级" },
            ["m_iTiresLevel"] = new FieldMeta { Label = "轮胎 等级", Group = "改装等级" },
            ["m_iHornLevel"] = new FieldMeta { Label = "喇叭 等级", Group = "改装等级" },

            // 成绩 / 统计
            ["m_fBestDeadOnTimeDistance"] = new FieldMeta { Label = "最佳 Dead on Time 距离", Group = "成绩与统计" },
            ["m_fBestPoliceStateDistance"] = new FieldMeta { Label = "最佳 Police State 距离", Group = "成绩与统计" },
            ["m_iBestHitCombo"] = new FieldMeta { Label = "最佳连击", Group = "成绩与统计" },
            ["m_iBestFlipFlop"] = new FieldMeta { Label = "最佳翻转", Group = "成绩与统计" },
            ["m_iNbKilledSoldiers"] = new FieldMeta { Label = "击杀士兵", Group = "成绩与统计" },
            ["m_iNbKilledCivilians"] = new FieldMeta { Label = "击杀平民", Group = "成绩与统计" },
            ["m_iNbZombieSplatterHits"] = new FieldMeta { Label = "撞碎丧尸", Group = "成绩与统计" },
            ["m_iNbDodgedSuperZombies"] = new FieldMeta { Label = "躲避超级丧尸", Group = "成绩与统计" },
            ["m_iNbDodgedBombing"] = new FieldMeta { Label = "躲避轰炸", Group = "成绩与统计" },
            ["m_completedAchievements"] = new FieldMeta { Label = "已完成成就", Group = "成绩与统计" },

            // 击败记录
            ["m_bDefeatedHoodNormalZombieMale"] = new FieldMeta { Label = "击败 兜帽男丧尸", Group = "击败记录" },
            ["m_bDefeatedHoodNormalZombieFemale"] = new FieldMeta { Label = "击败 兜帽女丧尸", Group = "击败记录" },
            ["m_bDefeatedHoodHardZombie"] = new FieldMeta { Label = "击败 困难丧尸", Group = "击败记录" },
            ["m_bDefeatedHoodVeryHardZombie"] = new FieldMeta { Label = "击败 极难丧尸", Group = "击败记录" },
            ["m_bDefeatedHoodSoldier"] = new FieldMeta { Label = "击败 兜帽士兵", Group = "击败记录" },

            // 见闻 / 教学
            ["m_bHaveSeenHelicopter"] = new FieldMeta { Label = "见过直升机", Group = "见闻与教学" },
            ["m_bHelicopterTutorialShown"] = new FieldMeta { Label = "直升机教学已显示", Group = "见闻与教学" },
            ["m_bHaveSeenSuperZombie"] = new FieldMeta { Label = "见过超级丧尸", Group = "见闻与教学" },
            ["m_bSuperZombieTutorialShown"] = new FieldMeta { Label = "超级丧尸教学已显示", Group = "见闻与教学" },
            ["m_bHaveSeenHoodFire"] = new FieldMeta { Label = "见过兜帽火焰", Group = "见闻与教学" },
            ["m_bHoodFireTutorialShown"] = new FieldMeta { Label = "兜帽火焰教学已显示", Group = "见闻与教学" },
            ["m_bHaveSeenHoodEnemy"] = new FieldMeta { Label = "见过兜帽敌人", Group = "见闻与教学" },
            ["m_bHoodEnemyTutorialShown"] = new FieldMeta { Label = "兜帽敌人教学已显示", Group = "见闻与教学" },
            ["m_bHaveSeenNoWindshieldDeath"] = new FieldMeta { Label = "见过无挡风死亡", Group = "见闻与教学" },
            ["m_bNoWindshieldDeathTutorialShown"] = new FieldMeta { Label = "无挡风死亡教学已显示", Group = "见闻与教学" },
            ["m_bPistolTutorialShown"] = new FieldMeta { Label = "手枪教学已显示", Group = "见闻与教学" },
            ["m_bPerceptionTutorialShown"] = new FieldMeta { Label = "感知教学已显示", Group = "见闻与教学" },
            ["m_bDeadOnTimeTutorialShown"] = new FieldMeta { Label = "Dead on Time 教学已显示", Group = "见闻与教学" },

            // 设置
            ["m_iQuality"] = new FieldMeta { Label = "画质", Group = "设置" },
            ["m_bMusic"] = new FieldMeta { Label = "音乐", Group = "设置" },
            ["m_bSound"] = new FieldMeta { Label = "音效", Group = "设置" },
            ["m_bDisplayBuildings"] = new FieldMeta { Label = "显示建筑", Group = "设置" },
            ["m_bPerceptionOn"] = new FieldMeta { Label = "感知开启", Group = "设置" },

            // 按键
            ["m_iLeftKey"] = new FieldMeta { Label = "按键：左转", Group = "按键" },
            ["m_iRightKey"] = new FieldMeta { Label = "按键：右转", Group = "按键" },
            ["m_iAccelKey"] = new FieldMeta { Label = "按键：加速", Group = "按键" },
            ["m_iBrakeKey"] = new FieldMeta { Label = "按键：刹车", Group = "按键" },
            ["m_iHandbrakeKey"] = new FieldMeta { Label = "按键：手刹", Group = "按键" },
            ["m_iHornKey"] = new FieldMeta { Label = "按键：喇叭", Group = "按键" },
            ["m_iWiperKey"] = new FieldMeta { Label = "按键：雨刷", Group = "按键" },
            ["m_iAttackKey"] = new FieldMeta { Label = "按键：攻击", Group = "按键" },
            ["m_iQualityKey"] = new FieldMeta { Label = "按键：画质", Group = "按键" },
            ["m_iPerceptionKey"] = new FieldMeta { Label = "按键：感知", Group = "按键" }
        };

        public static FieldMeta Get(string name)
        {
            return Map.TryGetValue(name, out var meta) ? meta : new FieldMeta { Label = name, Group = "其他" };
        }

        public static string GroupOrder(string group)
        {
            switch (group)
            {
                case "资源与进度": return "1";
                case "改装等级": return "2";
                case "成绩与统计": return "3";
                case "击败记录": return "4";
                case "见闻与教学": return "5";
                case "设置": return "6";
                case "按键": return "7";
                default: return "9";
            }
        }
    }
}
