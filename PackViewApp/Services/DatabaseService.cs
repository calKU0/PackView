using Microsoft.Data.SqlClient;
using PackViewApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PackViewApp.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<ProductInfo> GetProductInfo(string productId, long documentId)
        {
            ProductInfo product = new();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"SELECT TOP 1
                        Twr_GIDNumer AS [ProductId],
                        Twr_Kod AS [ProductCode],
                        Twr_Nazwa AS [ProductName],
	                    DAB_Dane as [ProductImage],
	                    DAB_Rozmiar as [ImageSize],
	                    TrE_JmZ as [ProductUnit],
                        CONVERT(DECIMAL(15,2), KP_IloscSpakowanegoTowaru) AS [QuantityPacked],
	                    CONVERT(DECIMAL(15,2), TrE_Ilosc) AS [QuantityToPack],
                        KP_DataUtworzenia AS [ScanDate],
	                    KP_DataWyslania AS [SentDate],
	                    IPKamery AS [Camera],
                        KP_Stanowisko as [Station],
                        Imie + ' ' + Nazwisko as [Operator]
                    FROM dbo.KontrolaPakowania WITH(NOLOCK)
                    JOIN dbo.KontrolaPakowaniaKamery WITH(NOLOCK) ON KP_Stanowisko = Stanowisko
					JOIN dbo.KontrolaPakowaniaOperatorzy WITH(NOLOCK) ON KP_OperatorPakujacy = Operator
                    JOIN cdn.TwrKarty WITH(NOLOCK) ON KP_Towar = Twr_GIDNumer
                    JOIN cdn.TraElem WITH(NOLOCK) ON KP_DokumentHandlowy = TrE_GIDNumer AND KP_Towar = TrE_TwrNumer
                    LEFT JOIN cdn.DaneObiekty WITH(NOLOCK) ON DAO_ObiNumer = KP_Towar AND DAO_ObiTyp = 16
                    LEFT JOIN cdn.DaneBinarne WITH(NOLOCK) ON DAO_DABId = DAB_ID AND dab_typid NOT IN (401, 1169, 343, 345, 344) AND DAB_Rozszerzenie in ('jpg','png','webp')
                    WHERE KP_DokumentHandlowy = @DocumentId AND KP_Towar = @ProductId
                    ORDER BY DAO_Pozycja ASC";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@DocumentId", documentId);
                        command.Parameters.AddWithValue("@ProductId", productId);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                product.ProductId = Convert.ToInt32(reader["ProductId"]);
                                product.ProductCode = reader["ProductCode"] as string;
                                product.ProductName = reader["ProductName"] as string;
                                product.ProductUnit = reader["ProductUnit"] as string;
                                if (reader["ProductImage"] != DBNull.Value && reader["ProductImage"] is byte[] imageBytes)
                                {
                                    product.ProductImage = imageBytes;
                                    product.ImageSize = Convert.ToInt64(reader["ImageSize"].ToString());
                                }

                                product.QuantityPacked = Convert.ToDecimal(reader["QuantityPacked"]);
                                product.QuantityToPack = Convert.ToDecimal(reader["QuantityToPack"]);

                                product.ScanDate = reader["ScanDate"] as DateTime?;
                                product.SentDate = reader["SentDate"] as DateTime?;
                                product.Camera = reader["Camera"] as string;
                                product.Station = reader["Station"] as string;
                                product.Operator = reader["Operator"] as string;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Wystąpił błąd przy zaczytywaniu produktu. Zawołaj administratora: {ex}");
            }

            return product;
        }
    }
}