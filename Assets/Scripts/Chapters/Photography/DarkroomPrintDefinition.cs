using UnityEngine;
namespace MemoryWhitebox
{
    [CreateAssetMenu(menuName="Memory/Photography/Darkroom Print")]
    public sealed class DarkroomPrintDefinition:ScriptableObject
    {
        [Min(.1f)] public float negativeDrySeconds=3,exposureSeconds=2,developerSeconds=4,stopSeconds=1,fixerSeconds=3,washSeconds=2,printDrySeconds=3;
        [Min(.01f)] public float dropTolerance=.14f;
        public float Duration(ParkPrintStage stage)
        {
            switch(stage)
            {
                case ParkPrintStage.NegativeDrying:return negativeDrySeconds;
                case ParkPrintStage.Exposing:return exposureSeconds;
                case ParkPrintStage.PaperDeveloper:return developerSeconds;
                case ParkPrintStage.PaperStop:return stopSeconds;
                case ParkPrintStage.PaperFixer:return fixerSeconds;
                case ParkPrintStage.PaperWash:return washSeconds;
                case ParkPrintStage.PaperDrying:return printDrySeconds;
                default:return 0;
            }
        }
    }
}
