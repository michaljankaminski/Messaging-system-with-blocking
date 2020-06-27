using EdcsClient.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace EdcsClient.Service
{
    public interface IDbService
    {
        public IEnumerable<User> GetUsers(int userId);
        public ObservableCollection<Message> GetThread(int from, int to);
        public IList<Message> GetThread(int threadId);
    }
    public class DbService : IDbService
    {
        private readonly string _connString;

        public DbService(IOptions<Settings> config)
        {
            _connString = config.Value.Postgres.ConnectionString;
        }
        public IEnumerable<User> GetUsers(int userId)
        {
            // Docelowo 'userId' ma służyć do pobierania listy 
            // kontaktów danego użytkownika, no ale póki co trzeba
            // to olać. 
            List<User> usersList = new List<User>();
            using (var conn = new NpgsqlConnection(_connString))
            {
                conn.Open();
                var getUsers = @"SELECT id, login FROM public.user 
                                WHERE id <> @userId";

                using (var cmd = new NpgsqlCommand(getUsers, conn))
                {
                    cmd.Parameters.AddWithValue("userId", userId);
                    var row = cmd.ExecuteReader();
                    while (row.Read())
                        usersList.Add(new User
                        {
                            Id = row.GetInt32(0),
                            Name = row.GetString(1)
                        });
                }
            }
            return usersList;
        }
        public ObservableCollection<Message> GetThread(int from, int to)
        {
            using (var conn = new NpgsqlConnection(_connString))
            {
                conn.Open();
                // na początku musimy odnaleźć czy taki 
                // wątek między tymi użytkownikami już istnieje
                // [TAK]: pobieramy całą listę dotychczasowych wiadomości
                // [NIE]: zwracamy nową, pustą listę wiadomości 
                int threadId = 0;
                string checkThread = @"SELECT id FROM thread WHERE 
                                (id_sender = @sender AND id_receiver = @receiver ) OR 
                                (id_receiver = @sender AND id_sender = @receiver)";

                using (var cmd = new NpgsqlCommand(checkThread, conn))
                {
                    cmd.Parameters.AddWithValue("sender", from);
                    cmd.Parameters.AddWithValue("receiver", to);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                            while (reader.Read())
                                threadId = reader.GetInt32(0);
                    }
                }

                if (threadId == 0)
                    return new ObservableCollection<Message>();
                else
                {
                    var msgList = new ObservableCollection<Message>();
                    string getMessages = @"SELECT * FROM message 
                                            WHERE id_thread = @thread 
                                            ORDER BY created DESC";

                    using (var cmd = new NpgsqlCommand(getMessages, conn))
                    {
                        cmd.Parameters.AddWithValue("thread", threadId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                                while (reader.Read())
                                    msgList.Add(new Message
                                    {
                                        ThreadId = threadId,
                                        Content = reader.GetString(3),
                                        Created = reader.GetDateTime(4),
                                        Modified = reader.GetDateTime(5),
                                        Sender = reader.GetInt32(2),
                                        Receiver = from == reader.GetInt32(2) ? to : from
                                    });
                        }
                    }
                    return msgList;
                }
            }
        }

        public IList<Message> GetThread(int threadId)
        {
            throw new NotImplementedException();
        }
    }
}
