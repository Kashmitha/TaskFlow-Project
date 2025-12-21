using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskFlow.API.Data;
using TaskFlow.API.DTOs;
using TaskFlow.API.Models;

namespace TaskFlow.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TasksController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/tasks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskResponseDto>>> GetTasks()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            IQueryable<TaskItem> query = _context.Tasks
                .Include(t => t.CreatedBy)
                .Include(t => t.AssignedTo);

            // If user is not admin, only show tasks created by or assigned to them
            if (userRole != "Admin")
            {
                query = query.Where(t => t.CreatedByUserId == userId || t.AssignedToUserId == userId);
            }

            var tasks = await query.Select(t => new TaskResponseDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Status = t.Status,
                Priority = t.Priority,
                DueDate = t.DueDate,
                CreatedAt = t.CreatedAt,
                CreatedByUserId = t.CreatedByUserId,
                CreatedByName = t.CreatedBy.FullName,
                AssignedToUserId = t.AssignedToUserId,
                AssignedToName = t.AssignedTo != null ? t.AssignedTo.FullName : null
            }).ToListAsync();

            return Ok(tasks);
        }

        // GET: api/tasks/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<TaskResponseDto>> GetTask(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var task = await _context.Tasks
                .Include(t => t.CreatedBy)
                .Include(t => t.AssignedTo)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                return NotFound();
            }

            // Authorization check
            if (userRole != "Admin" && task.CreatedByUserId != userId && task.AssignedToUserId != userId)
            {
                return Forbid();
            }

            var response = new TaskResponseDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                Priority = task.Priority,
                DueDate = task.DueDate,
                CreatedAt = task.CreatedAt,
                CreatedByUserId = task.CreatedByUserId,
                CreatedByName = task.CreatedBy.FullName,
                AssignedToUserId = task.AssignedToUserId,
                AssignedToName = task.AssignedTo?.FullName
            };

            return Ok(response);
        }

        // POST: api/tasks
        [HttpPost]
        public async Task<ActionResult<TaskResponseDto>> CreateTask([FromBody] CreateTaskDto createTaskDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            var task = new TaskItem
            {
                Title = createTaskDto.Title,
                Description = createTaskDto.Description,
                Status = createTaskDto.Status,
                Priority = createTaskDto.Priority,
                DueDate = createTaskDto.DueDate,
                CreatedByUserId = userId,
                AssignedToUserId = createTaskDto.AssignedToUserId
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            var createdTask = await _context.Tasks
                .Include(t => t.CreatedBy)
                .Include(t => t.AssignedTo)
                .FirstAsync(t => t.Id == task.Id);

            var response = new TaskResponseDto
            {
                Id = createdTask.Id,
                Title = createdTask.Title,
                Description = createdTask.Description,
                Status = createdTask.Status,
                Priority = createdTask.Priority,
                DueDate = createdTask.DueDate,
                CreatedAt = createdTask.CreatedAt,
                CreatedByUserId = createdTask.CreatedByUserId,
                CreatedByName = createdTask.CreatedBy.FullName,
                AssignedToUserId = createdTask.AssignedToUserId,
                AssignedToName = createdTask.AssignedTo?.FullName
            };

            return CreatedAtAction(nameof(GetTask), new { id = task.Id }, response);
        }

        // PUT: api/tasks/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] UpdateTaskDto updateTaskDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var task = await _context.Tasks.FindAsync(id);

            if (task == null)
            {
                return NotFound();
            }

            // Authorization check
            if (userRole != "Admin" && task.CreatedByUserId != userId)
            {
                return Forbid();
            }

            // Update only provided fields
            if (updateTaskDto.Title != null) task.Title = updateTaskDto.Title;
            if (updateTaskDto.Description != null) task.Description = updateTaskDto.Description;
            if (updateTaskDto.Status != null) task.Status = updateTaskDto.Status;
            if (updateTaskDto.Priority != null) task.Priority = updateTaskDto.Priority;
            if (updateTaskDto.DueDate.HasValue) task.DueDate = updateTaskDto.DueDate;
            if (updateTaskDto.AssignedToUserId.HasValue) task.AssignedToUserId = updateTaskDto.AssignedToUserId;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/tasks/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var task = await _context.Tasks.FindAsync(id);

            if (task == null)
            {
                return NotFound();
            }

            // Only admin or creator can delete
            if (userRole != "Admin" && task.CreatedByUserId != userId)
            {
                return Forbid();
            }

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}