import * as React from 'react';
import { ToastContainer, toast } from 'react-toastify';

export default class Notifier extends React.Component {
    constructor(props) {
        super(props);

        this.state = {
            ShowNotification: props.ShowNotification,
            NotificationMessage: props.NotificationMessage,
            NotificationSuccessful: props.NotificationSuccessful,
            OnOpened: props.OnOpened
        }

        this.raiseHasOpened = this.raiseHasOpened.bind(this);
    }
    
    componentWillUnmount() {
    }

    componentWillReceiveProps(props) {
        this.setState({
            ShowNotification: props.ShowNotification,
            NotificationMessage: props.NotificationMessage,
            NotificationSuccessful: props.NotificationSuccessful
        });
    }

    raiseHasOpened() {
        this.setState({ ShowNotification: false });
        if (this.state.OnOpened) {
            this.state.OnOpened();
        }
    }

    notify() {
        if (this.state.NotificationSuccessful) {
            toast.success(this.state.NotificationMessage, {
                position: toast.POSITION.TOP_RIGHT,
                onOpen: () => this.raiseHasOpened()
            });
        }
        else {
            toast.error(this.state.NotificationMessage, {
                position: toast.POSITION.TOP_RIGHT,
                onOpen: () => this.raiseHasOpened()
            });
        }
    } 
    
    render() {
        return (
            <div>
                {this.state.ShowNotification && this.notify()}

                <ToastContainer />

            </div>
        );



    }
}
