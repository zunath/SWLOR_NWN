import * as React from 'react';
import GameTopic from './shared/GameTopic';

export default class Rules extends React.Component {

    render() {
        return (
            <div>

                <div>
                    <div className="row">
                        <div className="col-12">
                            <div className="card border-primary mb-3 center">
                                <div className="card-body">
                                    <h4 className="card-title">Rules</h4>

                                    <p>
                                        We have a handful of very simple rules. Please read them before playing.
                                     </p>
                                    <p>
                                        By playing Star Wars: Legends of the Old Republic, you agree to abide by them at all times.
                                    </p>

                                </div>
                            </div>
                        </div>
                    </div>
                    
                    <GameTopic ViewModelName="RulesViewModel" />

                </div>
            </div>
        );
    }
}
