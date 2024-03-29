using TaskManager.Application.Exceptions;
using TaskManager.Application.Interface;
using Task = TaskManager.Domain.Entities.Task;

namespace TaskManager.Application.Commands
{
    public class RemoveTaskCommand : BaseCommand
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly int _id;

        public RemoveTaskCommand(IUnitOfWork unitOfWork, int id)
        {
            _unitOfWork = unitOfWork;
            _id = id;
        }

        public override void Execute()
        {
            var task = _unitOfWork.Tasks.Get(task => task.Id == _id);
            if (task == null) throw new TaskNotFoundException(_id);
            _unitOfWork.Tasks.Remove(task);
            _unitOfWork.SaveChanges();
        }

        public override void Validate()
        {
            if (_id <= 0) throw new TaskNotFoundException(_id);
        }
    }
}
