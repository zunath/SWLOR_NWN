import * as React from 'react';
import * as dotnetify from 'dotnetify';

export default class UserProfile extends React.Component {
    constructor(props) {
        super(props);
        this.vm = dotnetify.react.connect('UserProfileViewModel', this);
        this.state = { Username: '', AvatarURL: '', Email: '' }
    }

    componentWillUnmount() {
        this.vm.$destroy();
    }

    render() {
        return (
            <div>
                
                <h2 className="center">User Profile</h2>
                <div className="row">

                    <div className="col-2">
                        <img
                            className="img-fluid"
                            src={this.state.AvatarURL}
                            alt="profile" />
                    </div>
                    
                    <div className="col-9">
                        <div className="row">
                            <div className="col-12">

                                <div className="input-group">
                                    <span className="input-group-addon">
                                        Username:
                                    </span>
                                    <input type="text"
                                           className="form-control"
                                           disabled={true}
                                           value={this.state.Username} />
                                </div>
                            </div>
                            <div className="col-12">

                                <div className="input-group">
                                    <span className="input-group-addon">
                                        Email:
                                    </span>
                                    <input type="text"
                                           className="form-control"
                                           disabled={true}
                                           value={this.state.Email} />
                                </div>
                            </div>
                        </div>
                    </div>
                    
                    
                </div>
            
            </div>
        );



    }
}
