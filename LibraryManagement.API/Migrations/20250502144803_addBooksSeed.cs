using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LibraryManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class addBooksSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Books",
                columns: new[] { "Id", "Author", "AverageRating", "CategoryID", "CoverImageUrl", "CreatedAt", "Description", "ISBN", "IsDeleted", "PublicationYear", "Publisher", "RatingCount", "Title", "TotalQuantity", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "Harper Lee", 0.00m, 1, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "A novel about injustice in the American South.", null, false, null, null, 0, "To Kill a Mockingbird", 4, null },
                    { 2, "George Orwell", 0.00m, 1, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "A dystopian social science fiction novel and cautionary tale.", null, false, null, null, 0, "1984", 4, null },
                    { 3, "F. Scott Fitzgerald", 0.00m, 1, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "A story about the American dream.", null, false, null, null, 0, "The Great Gatsby", 4, null },
                    { 4, "Jane Austen", 0.00m, 1, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "A classic novel of manners.", null, false, null, null, 0, "Pride and Prejudice", 5, null },
                    { 5, "J.D. Salinger", 0.00m, 1, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "A story about teenage angst and alienation.", null, false, null, null, 0, "The Catcher in the Rye", 4, null },
                    { 6, "J.K. Rowling", 0.00m, 1, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "The first book in the Harry Potter series.", null, false, null, null, 0, "Harry Potter and the Sorcerer's Stone", 6, null },
                    { 7, "J.R.R. Tolkien", 0.00m, 1, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "A fantasy novel and children's book.", null, false, null, null, 0, "The Hobbit", 7, null },
                    { 8, "Stephen Hawking", 0.00m, 2, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "A landmark volume in science writing.", null, false, null, null, 0, "A Brief History of Time", 5, null },
                    { 9, "Carl Sagan", 0.00m, 2, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "Explores the universe and our place within it.", null, false, null, null, 0, "Cosmos", 2, null },
                    { 10, "Richard Dawkins", 0.00m, 2, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "A book on evolution centered on the gene.", null, false, null, null, 0, "The Selfish Gene", 5, null },
                    { 11, "Rachel Carson", 0.00m, 2, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "Documented the environmental harm caused by pesticides.", null, false, null, null, 0, "Silent Spring", 6, null },
                    { 12, "Charles Darwin", 0.00m, 2, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "Introduced the scientific theory of evolution by natural selection.", null, false, null, null, 0, "The Origin of Species", 3, null },
                    { 13, "Richard P. Feynman", 0.00m, 2, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "Anecdotes by the Nobel Prize-winning physicist.", null, false, null, null, 0, "Surely You're Joking, Mr. Feynman!", 2, null },
                    { 14, "James D. Watson", 0.00m, 2, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "An autobiographical account of the discovery of the structure of DNA.", null, false, null, null, 0, "The Double Helix", 6, null },
                    { 15, "Yuval Noah Harari", 0.00m, 3, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "An exploration of human history.", null, false, null, null, 0, "Sapiens: A Brief History of Humankind", 8, null },
                    { 16, "Jared Diamond", 0.00m, 3, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "Explores the reasons for Eurasian societies' dominance.", null, false, null, null, 0, "Guns, Germs, and Steel", 5, null },
                    { 17, "Anne Frank", 0.00m, 3, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "The writings from the diary kept by Anne Frank while she was in hiding.", null, false, null, null, 0, "The Diary of a Young Girl", 6, null },
                    { 18, "Howard Zinn", 0.00m, 3, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "Presents American history from the perspective of common people.", null, false, null, null, 0, "A People's History of the United States", 7, null },
                    { 19, "William L. Shirer", 0.00m, 3, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "A history of Nazi Germany.", null, false, null, null, 0, "The Rise and Fall of the Third Reich", 3, null },
                    { 20, "David McCullough", 0.00m, 3, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "Focuses on the events surrounding the start of the American Revolutionary War.", null, false, null, null, 0, "1776", 6, null },
                    { 21, "Thucydides", 0.00m, 3, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "An ancient Greek historical account of the war between Sparta and Athens.", null, false, null, null, 0, "The Peloponnesian War", 4, null },
                    { 22, "Daniel Kahneman", 0.00m, 4, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "Summarizes research on cognitive biases.", null, false, null, null, 0, "Thinking, Fast and Slow", 3, null },
                    { 23, "Viktor Frankl", 0.00m, 4, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "Details his experiences as a prisoner in Nazi concentration camps.", null, false, null, null, 0, "Man's Search for Meaning", 3, null },
                    { 24, "Robert Cialdini", 0.00m, 4, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "Examines key ways people can be influenced.", null, false, null, null, 0, "Influence: The Psychology of Persuasion", 6, null },
                    { 25, "Sigmund Freud", 0.00m, 4, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "Introduces Freud's theory of the unconscious with respect to dream interpretation.", null, false, null, null, 0, "The Interpretation of Dreams", 4, null },
                    { 26, "Mihaly Csikszentmihalyi", 0.00m, 4, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "Explores the concept of 'flow'.", null, false, null, null, 0, "Flow: The Psychology of Optimal Experience", 6, null },
                    { 27, "Susan Cain", 0.00m, 4, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "Argues that modern Western culture misunderstands and undervalues introverts.", null, false, null, null, 0, "Quiet: The Power of Introverts", 6, null },
                    { 28, "Dan Ariely", 0.00m, 4, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "Challenges assumptions about our ability to make rational decisions.", null, false, null, null, 0, "Predictably Irrational", 3, null },
                    { 29, "Adam Smith", 0.00m, 5, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "A fundamental work in classical economics.", null, false, null, null, 0, "The Wealth of Nations", 3, null },
                    { 30, "Steven D. Levitt & Stephen J. Dubner", 0.00m, 5, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "Explores the hidden side of everything using economic principles.", null, false, null, null, 0, "Freakonomics", 4, null },
                    { 31, "Thomas Piketty", 0.00m, 5, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "Analyzes wealth and income inequality.", null, false, null, null, 0, "Capital in the Twenty-First Century", 2, null },
                    { 32, "Daniel Kahneman", 0.00m, 5, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "Also relevant to behavioral economics.", null, false, null, null, 0, "Thinking, Fast and Slow", 3, null },
                    { 33, "John Maynard Keynes", 0.00m, 5, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "A central work of modern macroeconomic thought.", null, false, null, null, 0, "The General Theory of Employment, Interest and Money", 2, null },
                    { 34, "Richard H. Thaler & Cass R. Sunstein", 0.00m, 5, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "Discusses nudge theory in behavioral economics.", null, false, null, null, 0, "Nudge", 6, null },
                    { 35, "Abhijit V. Banerjee & Esther Duflo", 0.00m, 5, null, new DateTime(2025, 5, 2, 14, 48, 2, 827, DateTimeKind.Utc).AddTicks(1186), "A radical rethinking of the way we fight global poverty.", null, false, null, null, 0, "Poor Economics", 5, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 35);
        }
    }
}
