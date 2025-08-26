namespace DirectoryService.Contracts;

public record GetQuestionsDto(string Search, Guid[] TagIds, int Page, int Limit);