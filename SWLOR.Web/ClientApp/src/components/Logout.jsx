import * as React from 'react';

export default class Logout extends React.Component {

    constructor(props) {
        super(props);

        this.state = {
            callback: props.callback
        }
    }

    render() {
        return (
            <div>

                <div id="logoutModal" className="modal" role="dialog">
                    <div className="modal-dialog" role="document">
                        <div className="modal-content">
                            <div className="modal-header">
                                <h5 className="modal-title">Log out?</h5>
                                <button type="button" className="close" data-dismiss="modal" aria-label="Close">
                                    <span aria-hidden="true">&times;</span>
                                </button>
                            </div>
                            <div className="modal-body">
                                <p>Are you sure you want to log out?</p>
                            </div>
                            <div className="modal-footer">
                                <button type="button" className="btn btn-primary" onClick={this.state.callback}>Log Out</button>
                                <button type="button" className="btn btn-outline-primary" data-dismiss="modal">Cancel</button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        );



    }
}


