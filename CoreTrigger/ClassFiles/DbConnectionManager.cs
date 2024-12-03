using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using CoreTrigger.Extensions;

namespace CoreTrigger.ClassFiles
{
        public class DbConnectionManager
        {
                private MySqlConnection connection;
                private string connectionString;
                private string token;
                public DbConnectionManager(string connectionString, string token)
                {
                        this.connectionString = connectionString;
                        this.token = token;
                        connection = new MySqlConnection(splitConnectionString(connectionString, "").ToString());
                }
                public bool openConnection()
                {
                        try
                        {
                                connection.Open();
                                return true;
                        }
                        catch (MySqlException ex)
                        {
                                Console.WriteLine(ex);
                                return false;
                        }
                }
                public bool closeConnection()
                {
                        try
                        {
                                connection.Close();
                                return true;
                        }
                        catch (MySqlException ex)
                        {
                                Console.WriteLine(ex.Message);
                                return false;
                        }
                }
                public Queue<string> ConnectionStrings(string token)
                {
                        Queue<string> connectionStrings = new Queue<string>();
                        string query =
                                //token != "all"
                                //? $"select a.`connection` from attrib_account a where a.active = true and a.maintenance = false and a.token ='{token}' and a.`connection` not like \'%10.10.168.1%\' and a.`connection` not like \'cubed-hut-deploy\'"
                                //: "select a.`connection` from attrib_account a where a.active = true and a.maintenance = false and a.`connection` not like \'%10.10.168.1%\' and a.`connection` not like \'cubed-hut-deploy\'"
                                token != "all"
                                ? $"select a.`connection` from attrib_account a where a.active = true and a.maintenance = false and a.token ='{token}'"
                                : "select a.`connection` from attrib_account a where a.active = true and a.maintenance = false"
                        ;
                        connection = new MySqlConnection(splitConnectionString(connectionString, "attrib").ToString());
                        if (openConnection() == true)
                        {
                                using (MySqlCommand cmd = new MySqlCommand(query, connection))
                                {
                                        cmd.CommandTimeout = 15;
                                        MySqlDataReader dataReader = cmd.ExecuteReader();
                                        while (dataReader.Read())
                                        {
                                                connectionStrings.Enqueue(usernameFormatter(dataReader.GetString("connection")));
                                        }
                                        dataReader.Close();
                                }
                                closeConnection();
                        }
                        return connectionStrings;
                }
                public Queue<int> getProducts(string productId)
                {
                        Queue<int> productIds = new Queue<int>();
                        string query =
                                productId != "all_products"
                                ? $"select a.id from attrib_product a where a.active = 1 AND a.id = {productId}"
                                : "select a.id from attrib_product a where a.active = 1"
                        ;
                        connection = new MySqlConnection(splitConnectionString(connectionString, "").ToString());
                        if (openConnection() == true)
                        {
                                using (MySqlCommand cmd = new MySqlCommand(query, connection))
                                {
                                        cmd.CommandTimeout = 15;
                                        using (MySqlDataReader dataReader = cmd.ExecuteReader())
                                        {
                                                while (dataReader.Read())
                                                {
                                                        productIds.Enqueue(dataReader.GetInt16("id"));
                                                }
                                        }
                                }
                                connection.Close();
                        }
                        return productIds;
                }

                private MySqlConnectionStringBuilder splitConnectionString(string connectionString, string token)
                {
                        MySqlConnectionStringBuilder conn = new MySqlConnectionStringBuilder();
                        if (token == "")
                        {
                                string[] splitConString = Regex.Split(connectionString, "[^a-zA-Z0-9._-]+");
                                conn.UserID = splitConString[1];
                                conn.Password = splitConString[2];
                                conn.Server = splitConString[3];
                                conn.Database = splitConString[5];
                                conn.SslMode = MySqlSslMode.None;
                        }
                        else
                        {

                                LoginDetails.getUserName(out string userName, out string password, out string connServer,
                                        out string port);
                                conn.UserID = userName;
                                conn.Password = password;
                                conn.Server = connServer;
                                conn.Database = token;
                                conn.SslMode = MySqlSslMode.None;
                        }
                        return conn;
                }
                private string usernameFormatter(string conn)
                {
                    string[] splitConString = Regex.Split(conn, "[^a-zA-Z0-9._-]+");

                    return $"{splitConString[0]}://{splitConString[1]}:{splitConString[2]}@{splitConString[3]}:{splitConString[4]}/{splitConString[5]}";
                }
    }
}
