namespace LibraryManagement.API.Helpers
{
    public class PasswordHasher
    {
        public string HashPassword(string password)
        {
            // Work factor mặc định thường là đủ tốt
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 10); // Sử dụng work factor mặc định
        }

        public bool VerifyPassword(string providedPassword, string storedHash)
        {
            if (string.IsNullOrEmpty(providedPassword) || string.IsNullOrEmpty(storedHash))
            {
                return false; // Không thể xác thực nếu thiếu thông tin
            }
            try
            {
                return BCrypt.Net.BCrypt.Verify(providedPassword, storedHash);
            }
            catch (BCrypt.Net.SaltParseException) // Bắt lỗi nếu hash không hợp lệ
            {
                // Log lỗi ở đây nếu cần
                return false;
            }
        }
    }
}
