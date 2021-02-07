import * as React from 'react';

export default class NotFound extends React.Component {

    render() {
        return (
            <div className="container">
                <div className="row">
                    We're sorry but we weren't able to find that page.
                </div>
                
                <div className="row">&nbsp;</div>
                <div className="row"> 
                    <a className="btn btn-primary" href="/">Go Home</a>
                </div>
                
            </div>
        );



    }
}
