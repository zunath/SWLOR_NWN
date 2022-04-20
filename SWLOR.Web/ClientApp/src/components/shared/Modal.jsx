import * as React from 'react';
import { ToastContainer, toast } from 'react-toastify';

export default class Modal extends React.Component {
    constructor(props) {
        super(props);

        this.state = {
            Header: props.Header,
            Body: props.Body,
            ActionText: props.ActionText,
            Action: props.ActionCallback,
            ModalID: props.ModalID
        }
    }
    
    componentWillUnmount() {
    }

    componentWillReceiveProps(props) {
        this.setState({
            Header: props.Header,
            Body: props.Body,
            ActionText: props.ActionText,
            Action: props.ActionCallback,
            ModalID: props.ModalID
        });
    }
    
    render() {
        return (
            <div>
                <div
                    className="modal fade"
                    id={this.state.ModalID}
                    tabIndex="-1"
                    role="dialog"
                    aria-labelledby={this.state.ModalID + '_label'}
                    aria-hidden="true">
                    <div className="modal-dialog" role="document">
                        <div className="modal-content">
                            <div className="modal-header">
                                <h5 className="modal-title" id={this.state.ModalID + '_Label'}>{this.state.Header}</h5>
                                <button type="button" className="close" data-dismiss="modal" aria-label="Close">
                                    <span aria-hidden="true">&times;</span>
                                </button>
                            </div>
                            <div className="modal-body">
                                {this.state.Body}
                            </div>
                            <div className="modal-footer">
                                <button type="button"
                                        className="btn btn-primary"
                                        onClick={this.state.Action}
                                        data-dismiss="modal">
                                    {this.state.ActionText}
                                </button>
                                <button type="button"
                                        className="btn btn-outline-primary"
                                        data-dismiss="modal">
                                    Close
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        );



    }
}
