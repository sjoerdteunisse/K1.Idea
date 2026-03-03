using K1Idea.Application.Common.Tenancy;
using K1Idea.Application.Tickets.Commands;
using K1Idea.Domain.Entities;
using K1Idea.Domain.Enums;
using K1Idea.Domain.Interfaces;

namespace K1Idea.Application.Tests.Tickets;

public sealed class CreateTicketCommandHandlerTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ITicketRepository _tickets = Substitute.For<ITicketRepository>();
    private readonly IBusinessUnitRepository _bus = Substitute.For<IBusinessUnitRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _buId = Guid.NewGuid();

    private CreateTicketCommandHandler CreateHandler()
    {
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);
        var tenantCtx = new TenantContext { TenantId = _tenantId };
        var orgCtx = new OrgContext { OrgId = _orgId, UserId = _userId };
        return new CreateTicketCommandHandler(_uow, _tickets, _bus, tenantCtx, orgCtx, _clock);
    }

    [Fact]
    public async Task Handle_valid_idea_creates_ticket_and_owner_tbu()
    {
        _bus.GetByIdAsync(_tenantId, _orgId, _buId, Arg.Any<CancellationToken>())
            .Returns(new BusinessUnit { Id = _buId, TenantId = _tenantId, OrgId = _orgId, Slug = "eng", Name = "Engineering", CreatedAt = DateTimeOffset.UtcNow });
        _tickets.InsertAsync(Arg.Any<Ticket>(), Arg.Any<CancellationToken>()).Returns(Guid.NewGuid());

        var cmd = new CreateTicketCommand("Test Idea", null, TicketType.Idea, TicketPriority.Medium,
            TicketStatus.Backlog, null, null, _buId, []);

        var ticket = await CreateHandler().Handle(cmd, CancellationToken.None);

        ticket.Title.Should().Be("Test Idea");
        ticket.Type.Should().Be(TicketType.Idea);
        ticket.TenantId.Should().Be(_tenantId);

        // Owner BU row always inserted
        await _tickets.Received(1).InsertTicketBusinessUnitAsync(
            Arg.Is<TicketBusinessUnit>(t => t.BusinessUnitId == _buId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_unknown_business_unit_throws_not_found()
    {
        _bus.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), _buId, Arg.Any<CancellationToken>())
            .Returns((BusinessUnit?)null);

        var cmd = new CreateTicketCommand("X", null, TicketType.Idea, TicketPriority.Low,
            TicketStatus.Backlog, null, null, _buId, []);

        var act = () => CreateHandler().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<K1Idea.Application.Common.Exceptions.NotFoundException>();
    }
}
