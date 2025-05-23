﻿using LibraryManagement.API.Models.Enums;

namespace LibraryManagement.API.Models.DTOs.User
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public Gender Gender { get; set; } // Trả về Gender dạng enum (frontend sẽ nhận tên)
        public DateTime CreatedAt { get; set; }
    }
}
