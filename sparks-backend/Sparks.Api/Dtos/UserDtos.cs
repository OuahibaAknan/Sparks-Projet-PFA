using System.ComponentModel.DataAnnotations;
using Sparks.Api.Models;

namespace Sparks.Api.Dtos;

public record UserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    UserRole Role,
    UserStatus Status,
    UserAvailability Availability,
    int ActiveTickets,
    string Initials,
    string AvatarColor,
    DateTime CreatedAt
);

public record CreateUserDto(
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [Required, EmailAddress] string Email,
    UserRole Role);

public record UpdateUserDto(
    [MaxLength(100)] string? FirstName,
    [MaxLength(100)] string? LastName,
    UserRole? Role,
    UserStatus? Status,
    UserAvailability? Availability);

public record ResetPasswordDto(
    [Required] string NewPassword,
    [Required] string ConfirmPassword);