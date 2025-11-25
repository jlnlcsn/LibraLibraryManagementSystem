using System.Collections.Generic;
using System.ComponentModel;

namespace LibraLibraryManagementSystem.Models
{
    public class BookModel : INotifyPropertyChanged
    {
        private string _status;

        public int ID { get; set; }
        public string Author { get; set; }
        public string Title { get; set; }
        public string Edition { get; set; }
        public int? Volumes { get; set; }  // Changed to int?
        public int? Pages { get; set; }    // Changed to int?
        public string Publisher { get; set; }
        public int? Year { get; set; }     // Changed to int?
        public string Category { get; set; }
        public int ShelfLocation { get; set; }

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        // STATIC LIST USED BY ComboBoxColumn
        public static List<string> StatusOptions { get; } =
            new List<string>
            {
                "Available",
                "Borrowed",
                "Pending Shelving"
            };

        // STATUS NORMALIZATION
        public void ParseStatus()
        {
            if (string.IsNullOrWhiteSpace(Status))
            {
                Status = "Available";
                return;
            }

            string s = Status.Trim().ToLower();

            switch (s)
            {
                case "available":
                    Status = "Available";
                    break;
                case "borrowed":
                    Status = "Borrowed";
                    break;
                case "pending":
                case "pending shelving":
                    Status = "Pending Shelving";
                    break;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
