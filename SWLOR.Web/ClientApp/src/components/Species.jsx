import * as React from 'react';
import GameTopic from './shared/GameTopic';

export default class Species extends React.Component {

    render() {
        return (
            <div>

                <div>
                    <div className="row">
                        <div className="col-12">
                            <div className="card border-primary mb-3 center">
                                <div className="card-body">
                                    <h4 className="card-title">Species</h4>

                                    <p>
                                        The galaxy is full of strange and unique species.
                                        <br />
                                        You'll find information on the playable species below.
                                    </p>
                                </div>
                            </div>
                        </div>
                    </div>
                    
                    <GameTopic ViewModelName="SpeciesViewModel" />

                </div>
            </div>
        );
    }
}
