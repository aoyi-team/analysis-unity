public class Hero_101_Logic:BasePlayerLogic, ICollisionPatches
{
    public override void Init(_playerInfo info, int[] skillIds)
    {
        base.Init(info, skillIds);

    }
    public override void AttachCollisionEvents()
    {
    }
    public void OnFixedCollisionEnter(CollisionInfo info)
    {
        throw new System.NotImplementedException();
    }

    public void OnFixedCollisionExit(CollisionInfo info)
    {
        throw new System.NotImplementedException();
    }

    public void OnFixedCollisionStay(CollisionInfo info)
    {
        throw new System.NotImplementedException();
    }

    public void OnFixedTriggerEnter(CollisionInfo info)
    {
        throw new System.NotImplementedException();
    }

    public void OnFixedTriggerExit(CollisionInfo info)
    {
        throw new System.NotImplementedException();
    }

    public void OnFixedTriggerStay(CollisionInfo info)
    {
        throw new System.NotImplementedException();
    }
}