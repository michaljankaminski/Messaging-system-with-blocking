using EdcsClient.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace EdcsClient.Model.Enum
{
    public enum BanStatus
    {
        OUT,
        IN,
        NONE
    }
}

namespace EdcsClient.Service
{
    public interface IDbService
    {
        public EdcsClient.Model.Enum.BanStatus VerifyBan(int sender, int receiver);
        public User GetUser(string login, string password);
        public IEnumerable<User> GetUsers(int userId);
        public ObservableCollection<Message> GetThread(int from, int to);
        public IList<Message> GetThread(int threadId);
        public bool UpdateBan(int sender, int receiver);

    }
    public class DbService : IDbService
    {
        private readonly string _connString;

        public DbService(IOptions<Settings> config)
        {
            _connString = config.Value.Postgres.ConnectionString;
        }
        public User GetUser(string login, string password)
        {
            using (var conn = new NpgsqlConnection(_connString))
            {
                conn.Open();
                var getUsers = @"SELECT id, login FROM public.user 
                                WHERE login = @login AND password = @password";

                using (var cmd = new NpgsqlCommand(getUsers, conn))
                {
                    cmd.Parameters.AddWithValue("login", login);
                    cmd.Parameters.AddWithValue("password", password);

                    var row = cmd.ExecuteReader();
                    while (row.Read())
                        return new User
                        {
                            Id = row.GetInt32(0),
                            Name = row.GetString(1)
                        };
                }
            }

            return null;
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
                                            ORDER BY created ASC";

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
                                        Receiver = from == reader.GetInt32(2) ? to : from,
                                        CurrentUser = from == reader.GetInt32(2) ? true : false
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
        /// <summary>
        /// Weryfikowanie banów użytkowników. Wyróżniamy trzy typy 
        /// IN: Kiedy to ktoś nałożył bana na nas
        /// OUT: Kiedy to my kogoś zbanowaliśmy
        /// NONE: nie ma żadnego bana między użytkownikami
        /// </summary>
        /// <param name="sender">id</param>
        /// <param name="receiver">id</param>
        /// <returns></returns>
        public EdcsClient.Model.Enum.BanStatus VerifyBan(int sender, int receiver)
        {
            using (var conn = new NpgsqlConnection(_connString))
            {
                conn.Open();
                var getBan = @"SELECT * FROM public.ban 
                                WHERE (user_id = @from AND ban_user_id = @to) OR 
                                        (user_id = @to AND ban_user_id = @from)";

                using (var cmd = new NpgsqlCommand(getBan, conn))
                {
                    cmd.Parameters.AddWithValue("from", sender);
                    cmd.Parameters.AddWithValue("to", receiver);

                    var row = cmd.ExecuteReader();
                    if (row.HasRows)
                    {
                        while (row.Read())
                        {
                            if (row.GetInt32(0) == sender)
                                return EdcsClient.Model.Enum.BanStatus.OUT;
                            else if (row.GetInt32(0) == receiver)
                                return EdcsClient.Model.Enum.BanStatus.IN;
                        }
                    }
                    else
                        return EdcsClient.Model.Enum.BanStatus.NONE;
                }
            }
            return EdcsClient.Model.Enum.BanStatus.NONE;
        }
        public bool UpdateBan(int sender, int receiver)
        {
            using (var conn = new NpgsqlConnection(_connString))
            {
                conn.Open();

                bool isBan = false;
                string ban = String.Empty;
                string checkBan = @"SELECT * FROM ban WHERE user_id = @from AND ban_user_id = @to";

                using (var cmd = new NpgsqlCommand(checkBan, conn))
                {
                    cmd.Parameters.AddWithValue("from", sender);
                    cmd.Parameters.AddWithValue("to", receiver);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                            while (reader.Read())
                                isBan = true;
                    }
                }

                if (isBan == true)
                    ban = @"DELETE FROM public.ban WHERE user_id = @from AND ban_user_id = @to";
                else
                    ban = @"INSERT INTO public.ban (user_id, ban_user_id) VALUES (@from, @to)";

                using (var cmd = new NpgsqlCommand(ban, conn))
                {
                    cmd.Parameters.AddWithValue("from", sender);
                    cmd.Parameters.AddWithValue("to", receiver);

                    var row = cmd.ExecuteNonQuery();
                    if (row == 1)
                        return true;
                    else
                        return false;
                }

            }
        }
    }
}
