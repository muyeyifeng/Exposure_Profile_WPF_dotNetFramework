using Npgsql;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Exposure_Profile_WPF_dotNetFramework
{
    internal class PGsql
    {
        private static readonly string connString = "<>";

        public static bool ReadDatas(out Dictionary<int, string[]> keyValuePairs)
        {
            keyValuePairs = new();
            try
            {
                using (NpgsqlConnection conn = new(connString))
                {
                    conn.Open();
                    NpgsqlCommand cmd = new("SELECT * FROM profile.\"default\" WHERE \"STATUS\" = 'U'", conn);
                    NpgsqlDataReader npgsqlDataReader = cmd.ExecuteReader();
                    // uuid name profile uploadtime holder
                    while (npgsqlDataReader.Read())
                    {
                        string[] strings = new string[npgsqlDataReader.FieldCount];
                        strings[0] = npgsqlDataReader.GetGuid(0).ToString();
                        strings[1] = npgsqlDataReader.GetString(1);
                        strings[2] = npgsqlDataReader.GetString(2);
                        strings[3] = npgsqlDataReader.GetProviderSpecificValue(3).ToString();
                        strings[4] = npgsqlDataReader.GetString(4);
                        strings[5] = npgsqlDataReader.GetString(5);
                        keyValuePairs.Add(keyValuePairs.Count, strings);
                    }
                    return true;
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }
        public static bool InsertSomeData(Dictionary<string, string> insertValues)
        {
            try
            {
                using NpgsqlConnection conn = new(connString);
                conn.Open();
                if (insertValues.TryGetValue("name", out string name) && insertValues.TryGetValue("profile", out string profile) && insertValues.TryGetValue("holder", out string holder) && insertValues.TryGetValue("hash", out string hash))
                {
                    // uuid name profile uploadtime holder
                    string cmdstring = $"INSERT INTO profile.\"default\" (\"UUID\", \"NAME\", \"PROFILE\", \"UPLOADTIME\", \"HOLDER\", \"HASH\",\"STATUS\") VALUES ('{CreatUUID()}', '{name}', '{profile}', '{GetTimestamp()}', '{holder}','{hash}', 'U')";
                    NpgsqlCommand cmd = new(cmdstring, conn);
                    return cmd.ExecuteNonQuery() != -1;
                }
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        public static bool TaggingDeleteStatus(string uuid)
        {
            //UPDATE "profile"."default" SET "STATUS" = 'R' WHERE "UUID" = '3b54f160-dbf5-4a2c-bdb7-a88cc3d800b5'
            try
            {
                using NpgsqlConnection conn = new(connString);
                conn.Open();
                string cmdstring = $"UPDATE \"profile\".\"default\" SET \"STATUS\" = 'D' WHERE \"UUID\" = '{uuid}'";
                NpgsqlCommand cmd = new(cmdstring, conn);
                return cmd.ExecuteNonQuery() != -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        public static bool DeleteData(string uuid)
        {
            //DELETE FROM "profile"."default" WHERE "UUID" = '0b3a6e92-82f6-41d3-8ad1-e48ab73998fd'
            try
            {
                using NpgsqlConnection conn = new(connString);
                conn.Open();
                string cmdstring = $"DELETE FROM \"profile\".\"default\" WHERE \"UUID\" = '{uuid}'";
                NpgsqlCommand cmd = new(cmdstring, conn);
                return cmd.ExecuteNonQuery() != -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }
        public static string CreatUUID()
        {
            return Guid.NewGuid().ToString();
        }

        public static string GetTimestamp()
        {
            return DateTime.Now.ToString();
        }
    }
}
