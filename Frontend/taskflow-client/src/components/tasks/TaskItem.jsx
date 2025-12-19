import React from "react";

function TaskItem({ task, onEdit, onDelete }){
    const getStatusClass = (status) => {
        return `status-badge status-${status.toLowerCase()}`;
    };
    
    const getPriorityClass = (priority) => {
        return `priority-badge priority-${priority.toLowerCase()}`;
    };

    const formatDate = (date) => {
        if(!date) return 'No due date';
        return new Date(date).toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
        });
    };

    return (
        <div className="task-card">
            <div className="task-header">
                <h3>{task.title}</h3>
                <div className="task-badges">
                    <span className={getStatusClass(task.status)}>{task.status}</span>
                    <span className={getPriorityClass(task.priority)}>{task.priority}</span>
                </div>
            </div>

            <p className="task-description">{task.description || 'No description'}</p>

            <div className="task-meta">
                <div className="task-info">
                    <span className="info-label">Due Date:</span>
                    <span className="info-value">{formatDate(task.dueDate)}</span>
                </div>
                <div className="task-info">
                    <span className="info-label">Assigned To:</span>
                    <span className="info-value">{task.assignedToName || 'Unassigned'}</span>
                </div>
                <div className="task-info">
                    <span className="info-label">Created By:</span>
                    <span className="info-value">{task.createdByName}</span>
                </div>
            </div>

            <div className="task-actions">
                <button className="btn-edit" onClick={() => onEdit(task)}>
                    Edit
                </button>
                <button className="btn-delete" onClick={() => onDelete(task.id)}>
                    Delete
                </button>
            </div>
        </div>
    );
}

export default TaskItem;