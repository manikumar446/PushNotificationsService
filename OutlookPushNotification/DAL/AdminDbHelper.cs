using OutlookPushNotification.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace OutlookPushNotification.DAL
{
    public class AdminDbHelper
    {
        SqlConnectionStringBuilder builder = null;
        public AdminDbHelper()
        {
            builder = new SqlConnectionStringBuilder();
            builder.DataSource = "ciscowebex.database.windows.net";
            builder.UserID = "manikumar446";
            builder.Password = "Hyderabad@123";
            builder.InitialCatalog = "ciscowebex";
        }

        public User GetUser(string email)
        {
            User user = null;
            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                string query = "SELECT * FROM tenant_users WHERE email = @Email";
                SqlCommand sqlCommand = new SqlCommand();
                

                sqlCommand.Connection = connection;
                sqlCommand.CommandText = query;
                sqlCommand.CommandType = System.Data.CommandType.Text;
                sqlCommand.Parameters.Add("@Email", System.Data.SqlDbType.VarChar);
                sqlCommand.Parameters["@Email"].Value = email;
                connection.Open();
                SqlDataReader reader = sqlCommand.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        user = new User();
                        user.Email = reader["email"].ToString();
                        user.SubscriptionId = reader["subscription_id"].ToString();
                    }
                }
            }
            return user;
        }

        public User UpdateUser(string email, string subscriptionId=null)
        {
            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                var user = GetUser(email);
                string query = null;
                SqlCommand sqlCommand = new SqlCommand();

                sqlCommand.Connection = connection;
                sqlCommand.CommandType = System.Data.CommandType.Text;
                connection.Open();
                if (user != null)
                {
                    query = "UPDATE tenant_users SET subscription_id = @subscription_id WHERE email = @email";
                    sqlCommand.Parameters.Add("@subscription_id", System.Data.SqlDbType.VarChar);
                    sqlCommand.Parameters["@subscription_id"].Value = subscriptionId;
                    sqlCommand.Parameters.Add("@email", System.Data.SqlDbType.VarChar);
                    sqlCommand.Parameters["@email"].Value = email;
                }
                else
                {
                    query = "INSERT INTO tenant_users (email) VALUES (@Email)";
                    sqlCommand.Parameters.Add("@email", System.Data.SqlDbType.VarChar);
                    sqlCommand.Parameters["@email"].Value = email;
                }
                sqlCommand.CommandText = query;
                sqlCommand.ExecuteNonQuery();
            }
            return GetUser(email);
        }
    }
}