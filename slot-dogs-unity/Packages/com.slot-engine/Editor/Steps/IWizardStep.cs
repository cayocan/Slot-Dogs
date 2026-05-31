#if UNITY_EDITOR
namespace SlotEngine.Editor
{
public interface IWizardStep
{
    string Title   { get; }
    void   Draw    (SlotSetupData data);
    bool   Validate(SlotSetupData data, out string errorMessage);
}
}
#endif
