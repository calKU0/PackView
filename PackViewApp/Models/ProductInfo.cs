using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PackViewApp.Models
{
    public class ProductInfo
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public byte[] ProductImage { get; set; }
        public long ImageSize { get; set; }
        public string ProductUnit { get; set; }
        public decimal QuantityPacked { get; set; }
        public decimal QuantityToPack { get; set; }
        public DateTime? ScanDate { get; set; }
        public DateTime? SentDate { get; set; }
        public string Camera { get; set; }
        public string Station { get; set; }
        public string Operator { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string TempFilePath { get; set; }
        public bool IsRecordingComplete { get; set; } = false;
    }
}