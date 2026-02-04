using ERMS.Application.Features.Auth.Commands.CreateHRAccount;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;
using ERMS.Application.Interface;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Auth.Command.CreateHRAccount
{
    public class CreateHRAccountHandlerTest
    {
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly Mock<RoleManager<IdentityRole<Guid>>> _roleManagerMock;
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly CreateHRAccountHandler _handler;
        
        private Mock<DbSet<Enterprise>> _enterprisesMock;
        private Mock<DbSet<Employee>> _employeesMock;
        private Mock<DbSet<Department>> _departmentsMock;

        public CreateHRAccountHandlerTest()
        {
            var userStoreMock = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            var roleStoreMock = new Mock<IRoleStore<IdentityRole<Guid>>>();
            _roleManagerMock = new Mock<RoleManager<IdentityRole<Guid>>>(
                roleStoreMock.Object, null, null, null, null);

            _contextMock = new Mock<IERMSDbContext>();
            
            _enterprisesMock = CreateMockDbSet(new List<Enterprise>());
            _employeesMock = CreateMockDbSet(new List<Employee>());
            _departmentsMock = CreateMockDbSet(new List<Department>());

            _contextMock.Setup(x => x.Enterprises).Returns(_enterprisesMock.Object);
            _contextMock.Setup(x => x.Employees).Returns(_employeesMock.Object);
            _contextMock.Setup(x => x.Departments).Returns(_departmentsMock.Object);

            _handler = new CreateHRAccountHandler(
                _userManagerMock.Object,
                _roleManagerMock.Object,
                _contextMock.Object);
        }

        private static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> sourceList) where T : class
        {
            var queryable = sourceList.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();

            mockSet.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));

            mockSet.As<IQueryable<T>>()
                .Setup(m => m.Provider)
                .Returns(new TestAsyncQueryProvider<T>(queryable.Provider));

            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

            return mockSet;
        }

        [Fact]
        public async Task Handle_ShouldThrowInvalidOperationException_WhenEnterpriseNotFound()
        {
            // Arrange
            var command = new CreateHRAccountCommand { EnterpriseId = Guid.NewGuid() };
            

            
            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Doanh nghiệp không tồn tại.");
        }

        [Fact]
        public async Task Handle_ShouldThrowInvalidOperationException_WhenEmailExists()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            var enterprise = new Enterprise { Id = enterpriseId, IsDeleted = false, Status = "Active" };
            
            // Update the mock to contain data
            var enterprises = new List<Enterprise> { enterprise };
            _enterprisesMock = CreateMockDbSet(enterprises);
            _contextMock.Setup(x => x.Enterprises).Returns(_enterprisesMock.Object);

            var command = new CreateHRAccountCommand { EnterpriseId = enterpriseId, Email = "existing@example.com" };
            
            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(new User());

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Email đã được đăng ký.");
        }

        [Fact]
        public async Task Handle_ShouldCreateAccount_WhenSuccessful()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            var enterprise = new Enterprise { Id = enterpriseId, IsDeleted = false, Status = "Active", EnterpriseCode = "ENT", SubscriptionPlan = new SubscriptionPlan() };
            
            // Setup Enterprises
            var enterprises = new List<Enterprise> { enterprise };
            _enterprisesMock = CreateMockDbSet(enterprises);
            _contextMock.Setup(x => x.Enterprises).Returns(_enterprisesMock.Object);

            _departmentsMock = CreateMockDbSet(new List<Department>());
            _contextMock.Setup(x => x.Departments).Returns(_departmentsMock.Object);
            
            // Setup Employees (Empty initially)
            _employeesMock = CreateMockDbSet(new List<Employee>());
            _contextMock.Setup(x => x.Employees).Returns(_employeesMock.Object);

            var command = new CreateHRAccountCommand
            {
                EnterpriseId = enterpriseId,
                Email = "hr@example.com",
                Password = "Password123!",
                FullName = "HR Manager",
                PhoneNumber = "123456789"
            };

            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((User)null!);
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), command.Password))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            _roleManagerMock.Setup(x => x.RoleExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(true);
            
            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeEmpty();
            
            // Verify Add was called
            _employeesMock.Verify(x => x.Add(It.IsAny<Employee>()), Times.Once);
            _departmentsMock.Verify(x => x.Add(It.IsAny<Department>()), Times.Once);
            _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtMost(2));
        }
    }

    // Helper classes for Async Query Provider
    internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new TestAsyncEnumerable<TEntity>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression);
        }

        public object Execute(Expression expression)
        {
            return _inner.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            var expectedResultType = typeof(TResult).GetGenericArguments()[0];
            var executionResult = typeof(IQueryProvider)
                .GetMethod(
                    name: nameof(IQueryProvider.Execute),
                    genericParameterCount: 1,
                    types: new[] { typeof(Expression) })
                .MakeGenericMethod(expectedResultType)
                .Invoke(this, new[] { expression });

            return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))
                .MakeGenericMethod(expectedResultType)
                .Invoke(null, new[] { executionResult });
        }
    }

    internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable)
            : base(enumerable)
        { }

        public TestAsyncEnumerable(Expression expression)
            : base(expression)
        { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        IQueryProvider IQueryable.Provider
        {
            get { return new TestAsyncQueryProvider<T>(this); }
        }
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return new ValueTask<bool>(_inner.MoveNext());
        }

        public T Current
        {
            get { return _inner.Current; }
        }
    }
}
