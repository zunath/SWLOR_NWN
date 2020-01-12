import * as React from 'react';
import { Link } from 'react-router-dom';
import ChatLogs from './ChatLogs';
import ConnectionLogs from './ConnectionLogs';

export default class Logs extends React.Component {

    constructor(props) {
        super(props);

        this.state = {
            activeTab: '1'
        }
    }

    toggle(tab) {
        if (this.state.activeTab !== tab) {
            this.setState({
                activeTab: tab
            });
        }
    }

    render() {
        return <div>
            <ul className="nav nav-tabs" role="tablist">
                <li className="nav-item">
                    <Link className="nav-link active" data-toggle="tab" to="#nav-chatlogs" role="tab">Chat Logs</Link>
                </li>
                <li className="nav-item">
                    <Link className="nav-link" data-toggle="tab" to="#nav-connectionlogs" role="tab">Connection Logs</Link>
                </li>
            </ul>

            <div className="tab-content">
                <div className="tab-pane active" id="nav-chatlogs" role="tabpanel">
                    <ChatLogs />
                </div>
                <div className="tab-pane" id="nav-connectionlogs" role="tabpanel">
                    <ConnectionLogs />
                </div>
            </div>
            


        </div>;
    }
}