namespace PeopleStore;

internal readonly record struct Person
{
    public Person(string firstName, string lastName, int age)
    {
        FirstName = firstName;
        LastName = lastName;
        Age = age;
    }

    public string FirstName { get; init; }
    public string LastName { get; init; }
    public int Age { get; init; }

    public override string ToString()
    {
        return $"{FirstName} {LastName}, age {Age}";
    }
}
