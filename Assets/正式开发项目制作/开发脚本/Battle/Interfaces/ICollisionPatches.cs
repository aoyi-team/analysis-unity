public interface ICollisionPatches
{
    void OnFixedTriggerEnter(CollisionInfo info);
    void OnFixedTriggerExit(CollisionInfo info);
    void OnFixedTriggerStay(CollisionInfo info);
    void OnFixedCollisionEnter(CollisionInfo info);
    void OnFixedCollisionExit(CollisionInfo info);
    void OnFixedCollisionStay(CollisionInfo info);
}