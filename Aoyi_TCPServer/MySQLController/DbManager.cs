using System;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

//包含登录结果
public class LoginResult
{
	public int Result { get; set; }
	public int ErrType { get; set; }
}
/// <summary>
/// 数据库管理类
/// </summary>
public class DbManager {
	public static MySqlConnection mysql;
	static readonly JavaScriptSerializer Js = new JavaScriptSerializer();//转码器，序列化为string，反序列化为对应类型

	//连接mysql数据库
	public static bool Connect(string db, string ip, int port, string user, string pw)
	{
		//创建MySqlConnection对象
		mysql = new MySqlConnection();
		//连接参数
		string s = string.Format("Database={0};Data Source={1}; port={2};User Id={3}; Password={4}", 
			               db, ip, port, user, pw);
		mysql.ConnectionString = s;
		//连接
		try
		{
			mysql.Open();
			Console.WriteLine("[数据库]connect succ ");

			return true;
		}
		catch (Exception e)
		{
			Console.WriteLine("[数据库]connect fail, " + e.Message);
			return false;
		}
	}

	/// <summary>
	/// 数据库连接校验
	/// </summary>
	private static void CheckAndReconnect(){
		try{
			if(mysql.Ping()){
				return;
			}
			mysql.Close();
			mysql.Open();
			Console.WriteLine("[数据库] Reconnect!");
		}
		catch(Exception e){
			Console.WriteLine("[数据库] CheckAndReconnect fail " + e.Message);
		}
		
	}
	/// <summary>
	/// 用于判断名称是否存在
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	public static bool IsNameExist(string name)
	{
        CheckAndReconnect();//检查连接是否正常
                            //防sql注入
        if (!DbManager.IsSafeString(name))
        {
            return false;
        }
		string nameQuery = "SELECT * FROM account WHERE name=@Name;";
		try
		{
			using(MySqlCommand nameCmd=new MySqlCommand(nameQuery,mysql))
			{
				nameCmd.Parameters.AddWithValue("@Name", name);
				MySqlDataReader reader= nameCmd.ExecuteReader();
				bool hasRows= reader.HasRows;
				reader.Close();
				return hasRows;
			}
		}
		catch(Exception ex)
		{
			Console.WriteLine("[数据库]FindName Fail," + ex.Message);
			return false;
		}
    }
	/// <summary>
	/// 校验字符串是否符合
	/// </summary>
	/// <param name="str"></param>
	/// <returns></returns>
	private static bool IsSafeString(string str)
	{
		return !Regex.IsMatch(str, @"[-|;|,|\/|\(|\)|\[|\]|\}|\{|%|@|\*|!|\']");
	}
	/// <summary>
	/// 用于验证是否存在该名称
	/// </summary>
	/// <param name="id"></param>
	/// <returns></returns>
	public static bool IsAccountExist(string id)
	{
		CheckAndReconnect();//检查连接是否正常
		//防sql注入
		if (!DbManager.IsSafeString(id)){
			return false;
		}
		//sql语句
		string s = string.Format("select * from account where id='{0}';", id);//Select*表示选择所有列 from account表示查询的表为account where id={0}表示只选择id=指定值的行.
		//查询
		try 
		{
			MySqlCommand cmd = new MySqlCommand (s, mysql); 
			MySqlDataReader dataReader = cmd.ExecuteReader (); 
			bool hasRows = dataReader.HasRows;
			dataReader.Close();
			return hasRows;
		}
		catch(Exception e)
		{
			Console.WriteLine("[数据库] IsSafeString err, " + e.Message);
			return false;
		}
	}
	/// <summary>
	/// 注册功能,只需要记录密码，客户端已经验证过
	/// 自动获取当前account存在的最大id，取最大id并加1，例如000001
	/// </summary>
	/// <param name="id"></param>
	/// <param name="pw"></param>
	/// <returns></returns>
	public static string Register(string pw)
	{
		CheckAndReconnect();
		//防sql注入
		if(!DbManager.IsSafeString(pw)){
			Console.WriteLine("[数据库] Register fail, pw not safe");
			return null;
		}
        //创建account中的id
        string countQuery = "SELECT COUNT(*) FROM account";
		string generateID;
        try {
            using (MySqlCommand cmd = new MySqlCommand(countQuery, mysql))
            {
                int RowCount = Convert.ToInt32(cmd.ExecuteScalar());
                //创建六位数id
                generateID = (RowCount + 1).ToString("D6");
                //插入到数据库中
                string insertQuery = "INSERT INTO account SET id=@Id,name=@Name,pw=@Pw;";
                using (MySqlCommand insertcmd = new MySqlCommand(insertQuery, mysql))//执行插入账号语句
                {
                    insertcmd.Parameters.AddWithValue("@Id", generateID);
					insertcmd.Parameters.AddWithValue("@Name", "玩家" + generateID);
                    insertcmd.Parameters.AddWithValue("@Pw", pw);
                    insertcmd.ExecuteNonQuery();
                }
            }
			return generateID;
        }
		catch(Exception ex)
		{
            Console.WriteLine("[数据库] Register fail " + ex.Message);
            return null;
        }
	}
	/// <summary>
	/// 更新角色名
	/// </summary>
	public static bool UpdateAccountName(string id,string name)
	{
		CheckAndReconnect();
		if(!DbManager.IsSafeString(name))
		{
            Console.WriteLine("[数据库] UpdateName False, id not safe");
            return false;
        }
		//更新对应ID的name值
		string updateNameQuery = "UPDATE account SET name=@NewName WHERE id=@ID;";
		try
		{
            using (MySqlCommand NameCmd = new MySqlCommand(updateNameQuery, mysql))
            {
				NameCmd.Parameters.AddWithValue("@NewName", name);
				NameCmd.Parameters.AddWithValue("@ID", id);
				int rowsAffected= NameCmd.ExecuteNonQuery();
				return rowsAffected > 0;
            }
        }
		catch(Exception e)
		{
			Console.WriteLine("[数据库] UpdateName False,"+e.Message);
			return false;
		}
	}
	/// <summary>
	/// 登录功能,根据LoginWay来进行ID或者Name登录校验
	/// </summary>
	/// <param name="LoginWay"></param>
	/// <param name="id"></param>
	/// <param name="name"></param>
	/// <param name="pw"></param>
	/// <returns></returns>
    public static LoginResult LoginCheck(int LoginWay, string id, string name, string pw)
    {
        CheckAndReconnect();
        //防sql注入
        if (!DbManager.IsSafeString(id))
        {
            Console.WriteLine("[数据库] CheckPassword fail, id not safe");
            return new LoginResult { Result = 1 };
        }
        if (!DbManager.IsSafeString(pw))
        {
            Console.WriteLine("[数据库] CheckPassword fail, pw not safe");
            return new LoginResult { Result = 1 };
        }
        LoginResult loginResult = new LoginResult { Result = 0 };
        //ID查询
        if (LoginWay == 0)
        {
			if(!IsAccountExist(id))
			{
				loginResult.Result = 1;
				loginResult.ErrType = 1;
				return loginResult;
			}
			string idLoginQuery = "SELECT * FROM account WHERE id=@Id AND pw=@Pw;";
			try
			{
				using(MySqlCommand cmd = new MySqlCommand (idLoginQuery, mysql))
				{
					cmd.Parameters.AddWithValue("@Id", id);
					cmd.Parameters.AddWithValue("@Pw", pw);
					using(MySqlDataReader reader = cmd.ExecuteReader())
					{
						if(reader.HasRows)
						{
							loginResult.Result = 0;
							return loginResult;
						}
						else
						{
							loginResult.Result = 1;
							loginResult.ErrType = 0;
							return loginResult;
						}
					}
				}
			}
			catch(Exception ex)
			{
				Console.WriteLine("[数据库]LoginFail,"+ex.Message);
				loginResult.Result = 1;
				return loginResult;
			}
        }
		//角色名查找
		if (LoginWay == 1)
        {
            if (!IsNameExist(name))
            {
                loginResult.Result = 1;
                loginResult.ErrType = 1;
                return loginResult;
            }
            string nameLoginQuery = "SELECT * FROM account WHERE name=@Name AND pw=@Pw;";
            try
            {
                using (MySqlCommand cmd = new MySqlCommand(nameLoginQuery, mysql))
                {
                    cmd.Parameters.AddWithValue("@Name", name);
                    cmd.Parameters.AddWithValue("@Pw", pw);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            loginResult.Result = 0;
                            return loginResult;
                        }
                        else
                        {
                            loginResult.Result = 1;
                            loginResult.ErrType = 0;
                            return loginResult;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[数据库]LoginFail," + ex.Message);
                loginResult.Result = 1;
                return loginResult;
            }
        }
		return loginResult;
    }
}


