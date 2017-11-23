using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using OutlookPushNotification.Models;

namespace OutlookPushNotification.DAL
{
    public class DbHelper
    {
        SqlConnectionStringBuilder builder = null;
        public DbHelper()
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
                string query = "SELECT * FROM UserInfo WHERE email = @Email";
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
                        user.Id = Convert.ToInt32(reader["id"]);
                        user.AccessToken = reader["access_token"].ToString();
                        user.RefreshToken = reader["refresh_token"].ToString();
                        user.SubscriptionId = reader["subscriptionId"].ToString();
                        user.Scope = reader["scope"].ToString();
                    }
                }
            }
            return user;
        }

        public User GetUserBySubscriptionId(string subscriptionId)
        {
            User user = null;
            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                string query = "SELECT * FROM UserInfo WHERE subscriptionId = @subscriptionId";
                SqlCommand sqlCommand = new SqlCommand();


                sqlCommand.Connection = connection;
                sqlCommand.CommandText = query;
                sqlCommand.CommandType = System.Data.CommandType.Text;
                sqlCommand.Parameters.Add("@subscriptionId", System.Data.SqlDbType.VarChar);
                sqlCommand.Parameters["@subscriptionId"].Value = subscriptionId;
                connection.Open();
                SqlDataReader reader = sqlCommand.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        user = new User();
                        user.Email = reader["email"].ToString();
                        user.Id = Convert.ToInt32(reader["id"]);
                        user.AccessToken = reader["access_token"].ToString();
                        user.RefreshToken = reader["refresh_token"].ToString();
                        user.SubscriptionId = reader["subscriptionId"].ToString();
                        user.Scope = reader["scope"].ToString();
                    }
                }
            }
            return user;
        }

        public int UpdateTokens(string refreshToken, string accessToken, int id)
        {
            int rowsCount = 0;
            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                string query = "UPDATE UserInfo SET access_token = @access_token, refresh_token = @refresh_token WHERE id = @id";
                SqlCommand sqlCommand = new SqlCommand();

                sqlCommand.Connection = connection;
                sqlCommand.CommandText = query;
                sqlCommand.CommandType = System.Data.CommandType.Text;
                sqlCommand.Parameters.Add("@access_token", System.Data.SqlDbType.VarChar);
                sqlCommand.Parameters["@access_token"].Value = accessToken;
                sqlCommand.Parameters.Add("@refresh_token", System.Data.SqlDbType.VarChar);
                sqlCommand.Parameters["@refresh_token"].Value = refreshToken;
                sqlCommand.Parameters.Add("@id", System.Data.SqlDbType.Int);
                sqlCommand.Parameters["@id"].Value = id;
                connection.Open(); 
                rowsCount = sqlCommand.ExecuteNonQuery();
            }
            return rowsCount;
        }

        public User UpdateUser(string email, string accessToken, string refreshToken, string scope)
        {
            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                var user = GetUser(email);
                string query = null;
                SqlCommand sqlCommand = new SqlCommand();

                sqlCommand.Connection = connection;
                sqlCommand.CommandType = System.Data.CommandType.Text;
                connection.Open();
                if (user!=null)
                {
                    query = "UPDATE UserInfo SET access_token = @access_token, refresh_token = @refresh_token, scope = @scope WHERE email = @email";
                    sqlCommand.Parameters.Add("@access_token", System.Data.SqlDbType.VarChar);
                    sqlCommand.Parameters["@access_token"].Value = accessToken;
                    sqlCommand.Parameters.Add("@access_token", System.Data.SqlDbType.VarChar);
                    sqlCommand.Parameters["@refresh_token"].Value = refreshToken;
                    sqlCommand.Parameters.Add("@email", System.Data.SqlDbType.VarChar);
                    sqlCommand.Parameters["@email"].Value = email;
                    sqlCommand.Parameters.Add("@scope", System.Data.SqlDbType.VarChar);
                    sqlCommand.Parameters["@scope"].Value = scope;
                }
                else
                {
                    query = "INSERT INTO UserInfo (email,access_token,refresh_token,scope) VALUES (@Email,@access_token,@refresh_token,@scope)";
                    sqlCommand.Parameters.Add("@access_token", System.Data.SqlDbType.VarChar);
                    sqlCommand.Parameters["@access_token"].Value = accessToken;
                    sqlCommand.Parameters.Add("@refresh_token", System.Data.SqlDbType.VarChar);
                    sqlCommand.Parameters["@refresh_token"].Value = refreshToken;
                    sqlCommand.Parameters.Add("@email", System.Data.SqlDbType.VarChar);
                    sqlCommand.Parameters["@email"].Value = email;
                    sqlCommand.Parameters.Add("@scope", System.Data.SqlDbType.VarChar);
                    sqlCommand.Parameters["@scope"].Value = scope;
                }
                sqlCommand.CommandText = query;
                sqlCommand.ExecuteNonQuery();
            }
            return GetUser(email);
        }

        public int UpdateSubscriptionDetails(string email, string subscriptionId, string expirationDateTime)
        {
            int rowsCount = 0;
            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                string query = "UPDATE UserInfo SET subscriptionId = @subscriptionId, subscriptionExpirationDateTime = @subscriptionExpirationDateTime WHERE email = @email";
                SqlCommand sqlCommand = new SqlCommand();

                sqlCommand.Connection = connection;
                sqlCommand.CommandText = query;
                sqlCommand.CommandType = System.Data.CommandType.Text;
                sqlCommand.Parameters.Add("@subscriptionExpirationDateTime", System.Data.SqlDbType.VarChar);
                sqlCommand.Parameters["@subscriptionExpirationDateTime"].Value = expirationDateTime;
                sqlCommand.Parameters.Add("@subscriptionId", System.Data.SqlDbType.VarChar);
                sqlCommand.Parameters["@subscriptionId"].Value = subscriptionId;
                sqlCommand.Parameters.Add("@email", System.Data.SqlDbType.VarChar);
                sqlCommand.Parameters["@email"].Value = email;
                connection.Open();
                rowsCount = sqlCommand.ExecuteNonQuery();
            }
            return rowsCount;
        }

        public bool IsEventUpdatedByUser(string eventId)
        {
            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                string query = @"IF NOT EXISTS(SELECT * FROM EventUpdateHistory WHERE EventID = @EventID)
                                 BEGIN
                                    INSERT INTO EventUpdateHistory(EventID ,UpdatedBy) VALUES(@EventID,'USER')
                                 END ";
                SqlCommand sqlCommand = new SqlCommand();


                sqlCommand.Connection = connection;
                sqlCommand.CommandText = query;
                sqlCommand.CommandType = System.Data.CommandType.Text;
                sqlCommand.Parameters.Add("@EventID", System.Data.SqlDbType.VarChar);
                sqlCommand.Parameters["@EventID"].Value = eventId;
                connection.Open();
                sqlCommand.ExecuteNonQuery();

                query = "SELECT COUNT(ID) FROM EventUpdateHistory WHERE EventID = @EventID AND UpdatedBy='USER'";
                sqlCommand = new SqlCommand();
                sqlCommand.Connection = connection;
                sqlCommand.CommandText = query;
                sqlCommand.CommandType = System.Data.CommandType.Text;
                sqlCommand.Parameters.Add("@EventID", System.Data.SqlDbType.VarChar);
                sqlCommand.Parameters["@EventID"].Value = eventId;

                int count = Convert.ToInt16(sqlCommand.ExecuteScalar());
                if (count > 0)
                    return true;
                return false;
            }
        }

        public void UpdateEventHistory(string eventId, string updatedBy)
        {
            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                string query = "UPDATE EventUpdateHistory SET UpdatedBy=@UpdatedBy WHERE EventId=@EventId";
                SqlCommand sqlCommand = new SqlCommand();


                sqlCommand.Connection = connection;
                sqlCommand.CommandText = query;
                sqlCommand.CommandType = System.Data.CommandType.Text;
                sqlCommand.Parameters.Add("@EventID", System.Data.SqlDbType.VarChar);
                sqlCommand.Parameters["@EventID"].Value = eventId;
                sqlCommand.Parameters.Add("@UpdatedBy", System.Data.SqlDbType.VarChar);
                sqlCommand.Parameters["@UpdatedBy"].Value = updatedBy;
                connection.Open();
                sqlCommand.ExecuteNonQuery();
            }
        }
    }
}