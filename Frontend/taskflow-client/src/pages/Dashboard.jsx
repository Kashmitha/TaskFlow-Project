import React from 'react';
import TaskList from '../components/tasks/TaskList';
import './Dashboard.css';

function Dashboard() {
    return (
        <div className="dashboard">
            <TaskList />
        </div>
    );
}

export default Dashboard;
