﻿using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;

namespace SingleStoreExample
{
    public class Message
    {
        public long Id { get; set; }
        public string Content { get; set; }
        public DateTime CreateDate { get; set; }
    }

    public static class Program
    {

        // TODO: adjust these connection details to match your SingleStore deployment:
        public const string HOST = "localhost";
        public const int PORT = 3306;
        public const string USER = "root";
        public const string PASSWORD = "password_here";
        public const string DATABASE = "acme";

        public static void Main()
        {
            IDbConnection conn = new MySqlConnection();
            try
            {
                conn.ConnectionString = $"Server={HOST};Port={PORT};Uid={USER};Pwd={PASSWORD};Database={DATABASE};checkparameters=false;";
                // need `checkparameters=false` for stored procedures because there's no `mysql` database, it's named `information_schema` in SingleStore
                conn.Open();

                long id = Create(conn, "Inserted row");
                Console.WriteLine($"Inserted row id {id}");

                Message msg = ReadOne(conn, id);
                Console.WriteLine("Read one row:");
                if (msg == null)
                {
                    Console.WriteLine("not found");
                }
                else
                {
                    Console.WriteLine($"{msg.Id}, {msg.Content}, {msg.CreateDate}");
                }

                Update(conn, id, "Updated row");
                Console.WriteLine($"Updated row id {id}");

                List<Message> messages = ReadAll(conn);
                Console.WriteLine("Read all rows:");
                foreach (var message in messages)
                {
                    Console.WriteLine($"{message.Id}, {message.Content}, {message.CreateDate}");
                }

                Delete(conn, id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
            finally
            {
                conn.Close();
            }

        }

        private static long Create(IDbConnection conn, string content)
        {
            IDbCommand cmd = conn.CreateCommand();

            cmd.CommandText = "messages_create";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new MySqlParameter("@_content", MySqlDbType.String, 300) { Value = content });

            ulong id = (ulong)cmd.ExecuteScalar();
            cmd.Parameters.Clear(); // reusing this command? clean up the parameters.

            return (long)id;
        }

        private static Message ReadOne(IDbConnection conn, long id)
        {
            IDbCommand cmd = conn.CreateCommand();

            cmd.CommandText = "messages_read_by_id";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new MySqlParameter("@_id", MySqlDbType.Int64) { Value = id });

            Message result = null;
            using (IDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    result = new Message
                    {
                        Id = (long)reader["id"],
                        Content = (string)reader["content"],
                        CreateDate = (DateTime)reader["createdate"]
                    };
                }
            }

            return result;
        }

        private static List<Message> ReadAll(IDbConnection conn)
        {
            IDbCommand cmd = conn.CreateCommand();

            cmd.CommandText = "messages_read_all";
            cmd.CommandType = CommandType.StoredProcedure;

            List<Message> results = new List<Message>();
            using (IDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    results.Add(new Message
                    {
                        Id = (long)reader["id"],
                        Content = (string)reader["content"],
                        CreateDate = (DateTime)reader["createdate"]
                    });
                }
            }

            return results;
        }

        private static int Update(IDbConnection conn, long id, string content)
        {
            IDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "messages_update";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new MySqlParameter("@_content", MySqlDbType.VarChar, 300) { Value = content });
            cmd.Parameters.Add(new MySqlParameter("@_id", MySqlDbType.Int64) { Value = id });

            int rows = cmd.ExecuteNonQuery();

            return rows;
        }

        private static int Delete(IDbConnection conn, long id)
        {
            IDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "messages_delete";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new MySqlParameter("@_id", MySqlDbType.Int64) { Value = id });

            int rows = cmd.ExecuteNonQuery();

            return rows;
        }

    }
}
