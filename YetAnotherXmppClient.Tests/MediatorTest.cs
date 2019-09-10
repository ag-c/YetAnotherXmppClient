using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using YetAnotherXmppClient.Infrastructure;

namespace YetAnotherXmppClient.Tests
{
    public class Event1 : IEvent { }
    public class Query1 : IQuery<bool> { }

    public class MediatorTest
    {
        [Fact]
        public async Task Event()
        {
            var mediator = new Mediator();
            var evt = new Event1();
            var mock = new Mock<IEventHandler<Event1>>();
            mock.Setup(foo => foo.HandleEventAsync(It.IsAny<Event1>())).Returns(Task.CompletedTask);

            mediator.RegisterHandler(mock.Object);
            await mediator.PublishAsync(evt);

            mock.Verify(foo => foo.HandleEventAsync(evt), Times.Once());
        }

        [Fact]
        public async Task EventReplay()
        {
            var mediator = new Mediator();
            var evt = new Event1();
            var mock = new Mock<IEventHandler<Event1>>();
            mock.Setup(foo => foo.HandleEventAsync(It.IsAny<Event1>())).Returns(Task.CompletedTask);

            await mediator.PublishAsync(evt);
            mediator.RegisterHandler(mock.Object, true);

            mock.Verify(foo => foo.HandleEventAsync(evt), Times.Once());
        }

        [Fact]
        public async Task FuncQuery()
        {
            var mediator = new Mediator();

            mediator.RegisterHandler<Query1, bool>(qry => Task.FromResult(true));
            var result = await mediator.QueryAsync<Query1, bool>(new Query1());

            result.Should().BeTrue();
        }

        [Fact]
        public async Task FuncEvent()
        {
            var mediator = new Mediator();
            bool b = false;

            mediator.RegisterHandler<Event1>(evt => Method1(ref b));

            await mediator.PublishAsync(new Event1());

            b.Should().BeTrue();
        }

        Task Method1(ref bool b)
        {
            b = true;
            return Task.CompletedTask;
        }

        [Fact]
        public async Task QueryAsync()
        {
            var mediator = new Mediator();
            var query = new Query1();
            var mock = new Mock<IAsyncQueryHandler<Query1, bool>>();
            mock.Setup(foo => foo.HandleQueryAsync(It.IsAny<Query1>())).Returns(Task.FromResult(true));

            mediator.RegisterHandler(mock.Object);
            var result = await mediator.QueryAsync<Query1, bool>(query);

            mock.Verify(foo => foo.HandleQueryAsync(query), Times.Once());
            result.Should().BeTrue();
        }

        [Fact]
        public async Task Query()
        {
            var mediator = new Mediator();
            var query = new Query1();
            var mock = new Mock<IQueryHandler<Query1, bool>>();
            mock.Setup(foo => foo.HandleQuery(It.IsAny<Query1>())).Returns(true);

            mediator.RegisterHandler(mock.Object);
            var result = mediator.Query<Query1, bool>(query);

            mock.Verify(foo => foo.HandleQuery(query), Times.Once());
            result.Should().BeTrue();
        }
    }
}
