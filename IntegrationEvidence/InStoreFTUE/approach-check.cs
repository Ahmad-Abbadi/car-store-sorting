var c = UnityEngine.Object.FindFirstObjectByType<TutorialController>();
if (c.Step != TutorialController.Lesson.Match || !c.GuidanceTarget) throw new System.Exception("Matching stage missing.");
var handler = c.player.GetComponent<InteractionHandler>();
var target = c.GuidanceTarget;
var shelf = target.GetComponentInParent<SortingShelf>();
var data = (SortableItem_SO)c.player.ItemHandler.CurrentEquipped.Data;
if (!shelf.IsCorrectItem(data)) throw new System.Exception("Marker points to the wrong category.");
float eyeHeight = handler.SourceCamera.transform.position.y - c.player.transform.position.y;
bool found = false;
for (int i = 0; i < 16; i++)
{
    float angle = i * UnityEngine.Mathf.PI / 8;
    var position = target.position + new UnityEngine.Vector3(UnityEngine.Mathf.Cos(angle), 0, UnityEngine.Mathf.Sin(angle)) * 1.8f;
    position.y = c.player.transform.position.y;
    if (!TinyRiders.ThemedCollectionSession.Instance.roomLayout.Contains(position, .4f)) continue;
    var eye = position + UnityEngine.Vector3.up * eyeHeight;
    var aim = (target.position - eye).normalized;
    if (!UnityEngine.Physics.Raycast(eye, aim, out var hit, handler.MaxDistance, ~0, UnityEngine.QueryTriggerInteraction.Ignore) || hit.collider.GetComponentInParent<SortingShelf>() != shelf) continue;
    c.player.Motor.SetPosition(position);
    c.player.Look.SetLook(UnityEngine.Mathf.Atan2(aim.x, aim.z) * UnityEngine.Mathf.Rad2Deg, -UnityEngine.Mathf.Asin(aim.y) * UnityEngine.Mathf.Rad2Deg);
    found = true;
    break;
}
if (!found) throw new System.Exception("No visible QA approach to the matching shelf.");
return "Matching marker uses the real category; QA player approaches the existing shelf surface.";
