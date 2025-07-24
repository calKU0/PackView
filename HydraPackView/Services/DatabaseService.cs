using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HydraPackView.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public bool IsPacked(long id)
        {
            bool result = false;
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = connection.CreateCommand())
                    {
                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandText = @"IF EXISTS(Select 1 from dbo.KontrolaPakowania where KP_DokumentHandlowy = @Id)
                                                BEGIN SELECT 1 END ELSE BEGIN SELECT 0 END";
                        command.Parameters.AddWithValue("@Id", id);
                        result = Convert.ToBoolean(command.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd przy sprawdzaniu, czy faktura została spakowana. {ex.Message}");
            }

            return result;
        }

        public async Task<DataTable> GetPackingDataTable(long documentId)
        {
            DataTable result = new DataTable();
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
                    SELECT
                        Twr_GIDNumer AS [Id],
                        Twr_Kod AS [Kod],
                        Twr_Nazwa AS [Nazwa],
                        KP_IloscSpakowanegoTowaru AS [Il. spak.],
                        KP_DataSkanu AS [Data spak.]
                    FROM dbo.KontrolaPakowania
                    JOIN cdn.TwrKarty ON KP_Towar = Twr_GIDNumer
                    WHERE KP_DokumentHandlowy = @DocumentId
                    ORDER BY KP_DataSkanu ASC";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@DocumentId", documentId);

                        SqlDataAdapter adapter = new SqlDataAdapter(command);
                        adapter.Fill(result);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd przy sprawdzaniu, czy faktura została spakowana. {ex.Message}");
            }

            return result;
        }

        public (string, string, string) CheckPackingDates(long id)
        {
            (string, string, string) result = (string.Empty, string.Empty, string.Empty);

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = connection.CreateCommand())
                    {
                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandText = @"SELECT
                                                    CONVERT(varchar(30), DATEADD(HOUR, -4, DATEADD(SECOND, -30, MIN(KP_DataSkanu))), 126) + 'Z' AS [StartTime],
                                                    CONVERT(varchar(30), DATEADD(HOUR, -4, DATEADD(SECOND, 15, MAX(KP_DataWyslania))), 126) + 'Z' AS [EndTime],
                                                    IPKamery AS [camera]
                                                FROM dbo.KontrolaPakowania
                                                JOIN dbo.KontrolaPakowaniaKamery ON KP_Stanowisko = Stanowisko
                                                WHERE KP_DokumentHandlowy = @Id
                                                GROUP BY IPKamery;";
                        command.Parameters.AddWithValue("@Id", id);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result = (reader["StartTime"].ToString(), reader["EndTime"].ToString(), reader["Camera"].ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd przy sprawdzaniu, czy faktura została spakowana. {ex.Message}");
            }

            return result;
        }
    }
}