using LibraryManagement.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.API.Data.Configurations
{
    public class BookConfiguration : IEntityTypeConfiguration<Book>
    {
        public void Configure(EntityTypeBuilder<Book> builder)
        {
            builder.ToTable("Books");

            // Index Unique cho ISBN (chỉ áp dụng khi ISBN không NULL)
            builder.HasIndex(b => b.ISBN)
                   .IsUnique()
                   .HasFilter("[ISBN] IS NOT NULL"); // Cú pháp SQL Server, điều chỉnh nếu dùng DB khác

            // Chỉ định kiểu dữ liệu và độ chính xác cho AverageRating
            builder.Property(b => b.AverageRating)
                   .HasColumnType("decimal(3, 2)"); // Ví dụ: 3 chữ số tổng, 2 chữ số sau dấu phẩy

            // Cấu hình Soft Delete Filter: Mặc định chỉ query các sách chưa bị xóa
            builder.HasQueryFilter(b => !b.IsDeleted);

            // Cấu hình mối quan hệ với Category (chủ yếu để set OnDelete)
            builder.HasOne(b => b.Category)
                   .WithMany(c => c.Books)
                   .HasForeignKey(b => b.CategoryID)
                   .OnDelete(DeleteBehavior.Restrict); // Không cho xóa Category nếu còn Book

            // --- SEED DATA CHO BOOK (Dữ liệu thực tế hơn) ---
            var random = new Random();
            var utcNow = DateTime.UtcNow;
            var booksToSeed = new List<Book>();
            int currentBookId = 1; // Bắt đầu ID sách

            // --- Category 1: Fiction ---
            //booksToSeed.AddRange(new List<Book> {
            //    new Book { Id = currentBookId++, Title = "To Kill a Mockingbird", Author = "Harper Lee", CategoryID = 1, TotalQuantity = random.Next(3, 8), CreatedAt = utcNow, Description = "A novel about injustice in the American South." },
            //    new Book { Id = currentBookId++, Title = "1984", Author = "George Orwell", CategoryID = 1, TotalQuantity = random.Next(3, 8), CreatedAt = utcNow, Description = "A dystopian social science fiction novel and cautionary tale." },
            //    new Book { Id = currentBookId++, Title = "The Great Gatsby", Author = "F. Scott Fitzgerald", CategoryID = 1, TotalQuantity = random.Next(3, 8), CreatedAt = utcNow, Description = "A story about the American dream." },
            //    new Book { Id = currentBookId++, Title = "Pride and Prejudice", Author = "Jane Austen", CategoryID = 1, TotalQuantity = random.Next(3, 8), CreatedAt = utcNow, Description = "A classic novel of manners." },
            //    new Book { Id = currentBookId++, Title = "The Catcher in the Rye", Author = "J.D. Salinger", CategoryID = 1, TotalQuantity = random.Next(3, 8), CreatedAt = utcNow, Description = "A story about teenage angst and alienation." },
            //    new Book { Id = currentBookId++, Title = "Harry Potter and the Sorcerer's Stone", Author = "J.K. Rowling", CategoryID = 1, TotalQuantity = random.Next(5, 11), CreatedAt = utcNow, Description = "The first book in the Harry Potter series." },
            //    new Book { Id = currentBookId++, Title = "The Hobbit", Author = "J.R.R. Tolkien", CategoryID = 1, TotalQuantity = random.Next(3, 8), CreatedAt = utcNow, Description = "A fantasy novel and children's book." }
            //});

            //// --- Category 2: Science ---
            //booksToSeed.AddRange(new List<Book> {
            //    new Book { Id = currentBookId++, Title = "A Brief History of Time", Author = "Stephen Hawking", CategoryID = 2, TotalQuantity = random.Next(2, 7), CreatedAt = utcNow, Description = "A landmark volume in science writing." },
            //    new Book { Id = currentBookId++, Title = "Cosmos", Author = "Carl Sagan", CategoryID = 2, TotalQuantity = random.Next(2, 7), CreatedAt = utcNow, Description = "Explores the universe and our place within it." },
            //    new Book { Id = currentBookId++, Title = "The Selfish Gene", Author = "Richard Dawkins", CategoryID = 2, TotalQuantity = random.Next(2, 7), CreatedAt = utcNow, Description = "A book on evolution centered on the gene." },
            //    new Book { Id = currentBookId++, Title = "Silent Spring", Author = "Rachel Carson", CategoryID = 2, TotalQuantity = random.Next(2, 7), CreatedAt = utcNow, Description = "Documented the environmental harm caused by pesticides." },
            //    new Book { Id = currentBookId++, Title = "The Origin of Species", Author = "Charles Darwin", CategoryID = 2, TotalQuantity = random.Next(2, 7), CreatedAt = utcNow, Description = "Introduced the scientific theory of evolution by natural selection." },
            //    new Book { Id = currentBookId++, Title = "Surely You're Joking, Mr. Feynman!", Author = "Richard P. Feynman", CategoryID = 2, TotalQuantity = random.Next(2, 7), CreatedAt = utcNow, Description = "Anecdotes by the Nobel Prize-winning physicist." },
            //    new Book { Id = currentBookId++, Title = "The Double Helix", Author = "James D. Watson", CategoryID = 2, TotalQuantity = random.Next(2, 7), CreatedAt = utcNow, Description = "An autobiographical account of the discovery of the structure of DNA." }
            //});

            //// --- Category 3: History ---
            //booksToSeed.AddRange(new List<Book> {
            //    new Book { Id = currentBookId++, Title = "Sapiens: A Brief History of Humankind", Author = "Yuval Noah Harari", CategoryID = 3, TotalQuantity = random.Next(4, 9), CreatedAt = utcNow, Description = "An exploration of human history." },
            //    new Book { Id = currentBookId++, Title = "Guns, Germs, and Steel", Author = "Jared Diamond", CategoryID = 3, TotalQuantity = random.Next(3, 8), CreatedAt = utcNow, Description = "Explores the reasons for Eurasian societies' dominance." },
            //    new Book { Id = currentBookId++, Title = "The Diary of a Young Girl", Author = "Anne Frank", CategoryID = 3, TotalQuantity = random.Next(3, 8), CreatedAt = utcNow, Description = "The writings from the diary kept by Anne Frank while she was in hiding." },
            //    new Book { Id = currentBookId++, Title = "A People's History of the United States", Author = "Howard Zinn", CategoryID = 3, TotalQuantity = random.Next(3, 8), CreatedAt = utcNow, Description = "Presents American history from the perspective of common people." },
            //    new Book { Id = currentBookId++, Title = "The Rise and Fall of the Third Reich", Author = "William L. Shirer", CategoryID = 3, TotalQuantity = random.Next(2, 6), CreatedAt = utcNow, Description = "A history of Nazi Germany." },
            //    new Book { Id = currentBookId++, Title = "1776", Author = "David McCullough", CategoryID = 3, TotalQuantity = random.Next(3, 8), CreatedAt = utcNow, Description = "Focuses on the events surrounding the start of the American Revolutionary War." },
            //    new Book { Id = currentBookId++, Title = "The Peloponnesian War", Author = "Thucydides", CategoryID = 3, TotalQuantity = random.Next(2, 6), CreatedAt = utcNow, Description = "An ancient Greek historical account of the war between Sparta and Athens." }
            //});

            //// --- Category 4: Psychology ---
            //booksToSeed.AddRange(new List<Book> {
            //    new Book { Id = currentBookId++, Title = "Thinking, Fast and Slow", Author = "Daniel Kahneman", CategoryID = 4, TotalQuantity = random.Next(3, 8), CreatedAt = utcNow, Description = "Summarizes research on cognitive biases." },
            //    new Book { Id = currentBookId++, Title = "Man's Search for Meaning", Author = "Viktor Frankl", CategoryID = 4, TotalQuantity = random.Next(3, 8), CreatedAt = utcNow, Description = "Details his experiences as a prisoner in Nazi concentration camps." },
            //    new Book { Id = currentBookId++, Title = "Influence: The Psychology of Persuasion", Author = "Robert Cialdini", CategoryID = 4, TotalQuantity = random.Next(3, 8), CreatedAt = utcNow, Description = "Examines key ways people can be influenced." },
            //    new Book { Id = currentBookId++, Title = "The Interpretation of Dreams", Author = "Sigmund Freud", CategoryID = 4, TotalQuantity = random.Next(2, 6), CreatedAt = utcNow, Description = "Introduces Freud's theory of the unconscious with respect to dream interpretation." },
            //    new Book { Id = currentBookId++, Title = "Flow: The Psychology of Optimal Experience", Author = "Mihaly Csikszentmihalyi", CategoryID = 4, TotalQuantity = random.Next(3, 8), CreatedAt = utcNow, Description = "Explores the concept of 'flow'." },
            //    new Book { Id = currentBookId++, Title = "Quiet: The Power of Introverts", Author = "Susan Cain", CategoryID = 4, TotalQuantity = random.Next(3, 8), CreatedAt = utcNow, Description = "Argues that modern Western culture misunderstands and undervalues introverts." },
            //    new Book { Id = currentBookId++, Title = "Predictably Irrational", Author = "Dan Ariely", CategoryID = 4, TotalQuantity = random.Next(3, 8), CreatedAt = utcNow, Description = "Challenges assumptions about our ability to make rational decisions." }
            //});

            //// --- Category 5: Economics ---
            //booksToSeed.AddRange(new List<Book> {
            //    new Book { Id = currentBookId++, Title = "The Wealth of Nations", Author = "Adam Smith", CategoryID = 5, TotalQuantity = random.Next(2, 6), CreatedAt = utcNow, Description = "A fundamental work in classical economics." },
            //    new Book { Id = currentBookId++, Title = "Freakonomics", Author = "Steven D. Levitt & Stephen J. Dubner", CategoryID = 5, TotalQuantity = random.Next(3, 8), CreatedAt = utcNow, Description = "Explores the hidden side of everything using economic principles." },
            //    new Book { Id = currentBookId++, Title = "Capital in the Twenty-First Century", Author = "Thomas Piketty", CategoryID = 5, TotalQuantity = random.Next(2, 6), CreatedAt = utcNow, Description = "Analyzes wealth and income inequality." },
            //    new Book { Id = currentBookId++, Title = "Thinking, Fast and Slow", Author = "Daniel Kahneman", CategoryID = 5, TotalQuantity = random.Next(3, 8), CreatedAt = utcNow, Description = "Also relevant to behavioral economics." }, // Có thể trùng lặp nếu hợp lý
            //    new Book { Id = currentBookId++, Title = "The General Theory of Employment, Interest and Money", Author = "John Maynard Keynes", CategoryID = 5, TotalQuantity = random.Next(2, 6), CreatedAt = utcNow, Description = "A central work of modern macroeconomic thought." },
            //    new Book { Id = currentBookId++, Title = "Nudge", Author = "Richard H. Thaler & Cass R. Sunstein", CategoryID = 5, TotalQuantity = random.Next(3, 8), CreatedAt = utcNow, Description = "Discusses nudge theory in behavioral economics." },
            //    new Book { Id = currentBookId++, Title = "Poor Economics", Author = "Abhijit V. Banerjee & Esther Duflo", CategoryID = 5, TotalQuantity = random.Next(3, 8), CreatedAt = utcNow, Description = "A radical rethinking of the way we fight global poverty." }
            //});

            //// Khởi tạo giá trị mặc định cho các sách (đảm bảo tất cả thuộc tính được set)
            //foreach (var book in booksToSeed)
            //{
            //    book.AverageRating = 0.00m;
            //    book.RatingCount = 0;
            //    book.IsDeleted = false;
            //    // Đặt các giá trị nullable khác nếu cần, ví dụ:
            //    book.CoverImageUrl = null;
            //}

            //builder.HasData(booksToSeed);

        }
    }
}
