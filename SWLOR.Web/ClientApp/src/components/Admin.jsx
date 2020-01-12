import * as React from 'react';
import { Link } from 'react-router-dom';
import * as dotnetify from 'dotnetify';

export default class Admin extends React.Component {
    constructor(props) {
        super(props);
        this.vm = dotnetify.react.connect('AdminViewModel', this);
        this.state = { Role: 0 }
    }

    componentWillUnmount() {
        this.vm.$destroy();
    }

    render() {


        const self = this;
        function renderLogs() {

            if (self.state.Role === 1 || self.state.Role === 2) {
                return <div className="col">
                    <div className="card border-primary">
                        <div className="card-body center">
                            <h4 className="card-title">Logs</h4>
                            <p className="card-text">Search through log information collected on the server.</p>

                            <Link className="btn btn-primary" to="/admin/logs" role="button">
                                View Logs
                            </Link>
                        </div>

                    </div>
                </div>;
            }
            return '';
        }
        
        return (
            <div>

                <h2 className="center">Admin Toolkit</h2>

                <div className="row">
                    {renderLogs()}

                </div>


            </div>
        );



    }
}
