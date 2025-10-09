**TESTING.md**
Testing Guide
← Back to Main Documentation

Table of Contents

Unit Tests
Integration Tests
Test Naming Convention


Unit Tests
Test business logic in handlers:
csharppublic class CreateNoteCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidRequest_CreatesNote()
    {
        // Arrange
        var mockRepo = new Mock<INoteRepository>();
        var handler = new CreateNoteCommandHandler(mockRepo.Object, _mapper);
        var command = new CreateNoteCommand("Test Note", "Description");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        mockRepo.Verify(r => r.CreateNoteAsync(It.IsAny<Note>()), Times.Once);
    }
}

Integration Tests
Test repository against real database:
csharppublic class NoteRepositoryTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    [Fact]
    public async Task GetNotesAsync_ReturnsAllNotes()
    {
        // Arrange
        var repository = _fixture.CreateRepository<NoteRepository>();

        // Act
        var notes = await repository.GetNotesAsync(getAll: true, searchText: null);

        // Assert
        Assert.NotEmpty(notes);
    }
}

Test Naming Convention
textMethodName_Scenario_ExpectedResult

Examples:
- GetNoteById_ExistingId_ReturnsNote
- CreateNote_InvalidData_ThrowsValidationException
- DeleteNote_NonExistentId_ThrowsNotFoundException

← Back to Main Documentation | Next: Security Best Practices →
text---