import * as React from 'react';
import GameTopic from './shared/GameTopic';

export default class GameplayInfo extends React.Component {

    render() {
        return (
            <div>

                <div>
                    <div className="row">
                        <div className="col-12">
                            <div className="card border-primary mb-3 center">
                                <div className="card-body">
                                    <h4 className="card-title">Other Gameplay Information</h4>

                                    <p>
                                        Star Wars: Legends of the Old Republic offers a unique gameplay experience you won't find anywhere else.
                                         <br />
                                        There are numerous changes from vanilla Neverwinter Nights that you should be aware of.
                                         <br />
                                        Please take some time to read the questions below for more information.
                                     </p>
                                </div>
                            </div>
                        </div>
                    </div>
                    
                    <GameTopic ViewModelName="GameplayInfoViewModel" />

                </div>
            </div>
        );
    }
}
