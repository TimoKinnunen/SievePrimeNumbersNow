using SQLite;

namespace SievePrimeNumbersNow.Models
{
    [Table("PrimeNumbers")]
    public class PrimeNumberItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Unique]
        public int PrimeNumber { get; set; }
    }
}
