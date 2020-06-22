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
    }
    public class DbService : IDbService
    {
        private readonly IConfiguration _config;

        public DbService(IConfiguration config)
        {
            _config = config;
        }
        public bool SaveMessage(Message message)
        {
            using (var conn = new NpgsqlConnection(_config["Postgres:ConnectionString"]))
            {
                conn.Open();
                int threadId = 0;
                var checkThreadQuery = @"SELECT id FROM thread WHERE 
                                           (id_sender = @sender AND id_receiver = @receiver) 
                                        OR (id_sender = @receiver AND id_receiver = @sender)";

                var insertThread = @"INSERT INTO thread (id_sender, id_receiver, created, modified) 
                                            VALUES (@sender, @receiver, NOW(), NOW()) RETURNING id";

                var insertMessage = @"INSERT INTO message (id_thread, id_sender, content, created, modified) 
                                            VALUES (@thread, @sender, @content, NOW(), NOW())";

                using (var cmd = new NpgsqlCommand(checkThreadQuery, conn))
                {
                    cmd.Parameters.AddWithValue("sender", message.Sender);
                    cmd.Parameters.AddWithValue("receiver", message.Receiver);

                    threadId = (int)cmd.ExecuteScalar();
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

                using(var cmd = new NpgsqlCommand(insertMessage, conn))
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
