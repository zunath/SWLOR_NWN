import * as React from 'react';
import GameTopic from './shared/GameTopic';

export default class CharacterBackgrounds extends React.Component {
    render() {
        return (
            <div>
                <div className="row">
                    <div className="col-12">
                        <div className="card border-primary mb-3 center">
                            <div className="card-body">
                                <h4 className="card-title">Character Backgrounds</h4>

                                <p>
                                    There are no classes in Star Wars: Legends of the Old Republic. However, you may select a background for your character during creation.
                                    <br />
                                    These backgrounds give one temporary bonus and one permanent bonus.
                                    <br />
                                    Temporary bonuses are benefits that will eventually be outpaced. For example, starter weapons or a free upgrade in a perk.
                                    <br />
                                    Permanent bonuses are benefits that will <b>never</b> be outpaced. They will always provide benefit to your character in some fashion.
                                    <br />
                                    If you don't like the predefined backgrounds you are highly encouraged to create your own. We offer a 'Freelancer' background for this scenario.
                                </p>
                            </div>
                        </div>
                    </div>
                </div>

                <GameTopic ViewModelName="CharacterBackgroundViewModel" />

            </div>
        );



    }
}
