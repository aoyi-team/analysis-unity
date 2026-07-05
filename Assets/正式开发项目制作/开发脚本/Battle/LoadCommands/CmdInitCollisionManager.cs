using System.Collections;

public class CmdInitCollisionManager : ILoadCommand
{
    public string Name => "InitCollisionManager";

    public IEnumerator Execute(LoadContext ctx)
    {
        CollisionManager.Instance.Init(new FixMath.FixedVector2(-11, -11), new FixMath.FixedVector2(11, 11));
        yield break;
    }
}
