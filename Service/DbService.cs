using EdcsServer.Model;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;

namespace EdcsServer.Service
{
    public interface IDbService
    {
        public bool SaveMessage(Message message);
        public IEnumerable<int> GetUsersIds();
    }
    public class DbService : IDbService
    {
        private readonly IConfiguration _config;

        public DbService(IConfiguration config)
        {
            _config = config;
        }
        public IEnumerable<int> GetUsersIds()
        {
            List<int> usersList = new List<int>();
            using (var conn = new NpgsqlConnection(_config["Postgres:ConnectionString"]))
            {
                conn.Open();
                var getUsers = @"SELECT id FROM broker.public.user";

                using (var cmd = new NpgsqlCommand(getUsers, conn))
                {
                    var row = cmd.ExecuteReader();
                    while (row.Read())
                        usersList.Add(row.GetInt32(0));
                }
            }
            return usersList;
        }
        public bool SaveMessage(Message message)
        {
            using (var conn = new NpgsqlConnection(_config["Postgres:ConnectionString"]))
            {
                conn.Open();
                int threadId = 0;
                var checkThreadQuery = @"SELECT id FROM broker.public.thread WHERE 
                                           (id_sender = @sender AND id_receiver = @receiver) 
                                        OR (id_sender = @receiver AND id_receiver = @sender)";

                var insertThread = @"INSERT INTO broker.public.thread (id_sender, id_receiver, created, modified) 
                                            VALUES (@sender, @receiver, NOW(), NOW()) RETURNING id";

                var insertMessage = @"INSERT INTO broker.public.message (id_thread, id_sender, content, created, modified) 
                                            VALUES (@thread, @sender, @content, NOW(), NOW())";

                using (var cmd = new NpgsqlCommand(checkThreadQuery, conn))
                {
                    cmd.Parameters.AddWithValue("sender", message.Sender);
                    cmd.Parameters.AddWithValue("receiver", message.Receiver);

                    string thread = (string)cmd.ExecuteScalar();
                    threadId = Convert.ToInt32(thread);
                }

                if (threadId == 0)
                {
                    using (var cmd = new NpgsqlCommand(insertThread, conn))
                    {
                        cmd.Parameters.AddWithValue("sender", message.Sender);
                        cmd.Parameters.AddWithValue("receiver", message.Receiver);

                        threadId = (int)cmd.ExecuteScalar();
                    }
                }

                using (var cmd = new NpgsqlCommand(insertMessage, conn))
                {
                    cmd.Parameters.AddWithValue("thread", threadId);
                    cmd.Parameters.AddWithValue("sender", message.Sender);
                    cmd.Parameters.AddWithValue("content", message.Content);

                    var result = cmd.ExecuteNonQuery();
                }

            }

            return false;
        }
    }
}
