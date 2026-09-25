using System.ComponentModel.DataAnnotations;
using MenuOrdering.API.Enums;

namespace MenuOrdering.API.DTOs.Users;

public class CreateUserRequest
{
[Required]
public string FullName { get; set; } = string.Empty;


[Required]
[EmailAddress]
public string Email { get; set; } = string.Empty;

[Required]
[MinLength(6)]
public string Password { get; set; } = string.Empty;

[Required]
public UserRole Role { get; set; }


}
