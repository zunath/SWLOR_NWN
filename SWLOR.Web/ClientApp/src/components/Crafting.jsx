import * as React from 'react';
import GameTopic from './shared/GameTopic';

export default class Crafting extends React.Component {

    render() {
        return (
            <div>

                <div>
                    <div className="row">
                        <div className="col-12">
                            <div className="card border-primary mb-3 center">
                                <div className="card-body">
                                    <h4 className="card-title">Crafting </h4>

                                    <p>
                                        Star Wars: Legends of the Old Republic offers an extensive crafting system.
                                        <br />
                                        Select from the topics below to learn more about how it works!
                                    </p>
                                </div>
                            </div>
                        </div>
                    </div>
                    
                    <GameTopic ViewModelName="CraftingViewModel" />

                </div>
            </div>
        );
    }
}
