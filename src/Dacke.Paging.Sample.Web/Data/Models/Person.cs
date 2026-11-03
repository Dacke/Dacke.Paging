using System.ComponentModel.DataAnnotations;

namespace Dacke.Paging.Sample.Web.Data.Models;

public class Person {
    [Key]
    public Guid PersonId { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }

    public int Age { get; set; }

    public DateTime BirthDay { get; set; }


    public Person() => PersonId = Guid.CreateVersion7();
}