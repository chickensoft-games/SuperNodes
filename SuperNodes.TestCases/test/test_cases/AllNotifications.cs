
using Godot;
using SuperNodes.Types;

namespace AllNotifications;

[SuperNode]
public partial class Example : Node {
  private void OnPostinitialize() { }
  private void OnPredelete() { }
  private void OnNotification(int what) { }
  private void OnEnterTree() { }
  // not working
  private void OnWMWindowFocusIn() { }
  // not working
  private void OnWMWindowFocusOut() { }
  // not working
  private void OnWMCloseRequest() { }
  // not working
  private void OnWMSizeChanged() { }
  // not working
  private void OnWMDpiChange() { }
  private void OnVpMouseEnter() { }
  private void OnVpMouseExit() { }
  private void OnOsMemoryWarning() { }
  private void OnTranslationChanged() { }
  // not working
  private void OnWMAbout() { }
  private void OnCrash() { }
  private void OnOsImeUpdate() { }
  private void OnApplicationResumed() { }
  private void OnApplicationPaused() { }
  private void OnApplicationFocusIn() { }
  private void OnApplicationFocusOut() { }
  private void OnTextServerChanged() { }
  // not working
  private void OnWMMouseExit() { }
  // not working
  private void OnWMMouseEnter() { }
  // not working
  private void OnWMGoBackRequest() { }
  private void OnEditorPreSave() { }
  private void OnExitTree() { }
  private void OnMovedInParent() { }
  private void OnReady() { }
  private void OnEditorPostSave() { }
  private void OnUnpaused() { }
  private void OnPhysicsProcess(double delta) { }
  private void OnProcess(double delta) { }
  private void OnParented() { }
  private void OnUnparented() { }
  private void OnPaused() { }
  private void OnDragBegin() { }
  private void OnDragEnd() { }
  private void OnPathRenamed() { }
  private void OnInternalProcess() { }
  private void OnInternalPhysicsProcess() { }
  private void OnPostEnterTree() { }
  private void OnDisabled() { }
  private void OnEnabled() { }
  private void OnSceneInstantiated() { }


  public override partial void _Notification(int what);
}
