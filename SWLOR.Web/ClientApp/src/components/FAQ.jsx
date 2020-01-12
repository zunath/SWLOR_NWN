import * as React from 'react';
import GameTopic from './shared/GameTopic';

export default class FAQ extends React.Component {
    render() {
        return (
            <div>

                <div>
                    <div className="row">
                        <div className="col-12">
                            <div className="card border-primary mb-3 center">
                                <div className="card-body">
                                    <h4 className="card-title">Frequently Asked Questions</h4>

                                    <p>
                                        Have a question we didn't cover in the other sections? Check below!
                                     </p>
                                </div>
                            </div>
                        </div>
                    </div>
                    
                    <GameTopic ViewModelName="FAQViewModel" />

                </div>
            </div>
        );
    }
}
