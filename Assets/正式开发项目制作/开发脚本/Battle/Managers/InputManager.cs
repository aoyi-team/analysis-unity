using UnityEngine;

public class InputManager:MonoBehaviour
{
    private static InputManager instance;
    public static InputManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new GameObject("InputManager").AddComponent<InputManager>();
            }
            return instance;
        }
    }
    #region 本地输入采集相关变量
    private AnimState localState;
    private ActionCode localAction;
    private bool isMove;
    private int locaFlipx;
    private int _moveX;
    private int _moveY;

    private int targetX;
    private int targetY;
    #endregion
    private _playerInfo LocalInfo;
    public void Init()
    {
        LocalInfo=PlayerManager.Instance.PlayerInfo;
    }
    private void Update()
    {
        if (Application.isFocused == false) return;// 如果游戏窗口不在焦点上，就不采集输入，避免误操作
        if (Camera.main == null) return;
        if (LocalInfo == null) return;
        //Debug.Log("采集进行了");
        _moveX = (int)Input.GetAxisRaw("Horizontal");
        _moveY = (int)Input.GetAxisRaw("Vertical");
        if (_moveX != 0 || _moveY != 0)
        {
            isMove = true;
        }
        else isMove = false;
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = new Vector2(mousePos.x, mousePos.y) - LocalInfo._currLogicPos.ToVector2();
        float angle = Vector2.Angle(Vector2.down, dir);
        if (angle >= 145.0f)
        {
            localState = AnimState.up;
        }
        else localState = AnimState.side;
        float minus = mousePos.x - LocalInfo._currLogicPos.x.ToFloat();
        if (minus > 0.1f)
        {
            locaFlipx = 1;
        }
        else if (minus < -0.1f)
        {
            locaFlipx = -1;
        }
        // 后面再加入对于是否攻击或者释放技能的判断，这里先简单处理一下
        if (Input.GetMouseButtonDown(0))
        {
            localAction = ActionCode.Attack;
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            localAction = ActionCode.Skill;
        }// 这里的我的动作优先级是技能>攻击>移动，实际可以根据需要调整

    }
    private bool CanSendMove()
    {
        // 这里可以加入一些条件判断，例如当前是否在移动动画中，或者是否有其他状态限制等，来决定是否可以发送移动指令
        return true;
    }

    private bool CanSendAttack()
    {
        // 这里可以加入一些条件判断，例如当前是否在攻击动画中，或者技能冷却时间是否结束等，来决定是否可以发送攻击指令
        return true;
    }
    private bool CanSendSkill()
    {
        // 同样可以加入一些条件判断，例如当前是否在技能动画中，或者技能冷却时间是否结束等，来决定是否可以发送技能指令
        return true;
    }
    private bool CanSendAddedSkill()
    {
        return true;
    }

    public InputCollect ReturnCurrentFrameInput()
    {
        InputCollect inputCollect = new InputCollect
        {
            moveDirx = _moveX,
            moveDiry = _moveY,
            state = localState,
            code = localAction,
            flipx = locaFlipx,
            isMoving = isMove
        };
        localAction = ActionCode.None;// 发送完当前帧的操作后，重置为None，等待下一次输入
        return inputCollect;
    }
}
// 用于持续存储本地玩家的操作输入，例如普攻actionCode，这里的ActionCode和AnimState是本地存储的，不会立刻调用到服务器，服务器返回
// 操作到本地才进行操作变化，而不是立刻影响到逻辑层
public struct InputCollect
{
    // 鼠标点击的目标点坐标
    public int targetX;
    public int targetY;


    public int moveDirx;
    public int moveDiry;
    public AnimState state;
    public ActionCode code;
    public int flipx;
    public bool isMoving;
}