namespace Api.Models;

/// <summary>
/// Data model for a Todo item
/// </summary>
public class Todo
{
    // primary key for database
    public int Id { get; set; }

    // required title field with safe default
    public string Title { get; set; } = string.Empty;

    // boolean flag for completion status
    public bool Done { get; set; }
}