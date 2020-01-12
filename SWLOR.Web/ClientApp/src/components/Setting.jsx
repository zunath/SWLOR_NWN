import * as React from 'react';
import GameTopic from './shared/GameTopic';

export default class Setting extends React.Component {
    render() {
        return (
            <div>

                <div>
                    <div className="row">
                        <div className="col-12">
                            <div className="card border-primary mb-3 center">
                                <div className="card-body">
                                    <h4 className="card-title">Setting</h4>

                                    <p>
                                        Looking for some information on the module story? You've come to the right place.
                                        <br />
                                        Click on any of the topics below to learn more about the setting of SWLOR.
                                        <br />
                                        More information will be provided as we get closer to our official launch!
                                    </p>
                                </div>
                            </div>
                        </div>
                    </div>
                    
                    <GameTopic ViewModelName="SettingViewModel" />

                </div>
            </div>
        );
    }
}
