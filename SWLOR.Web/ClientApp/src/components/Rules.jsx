import * as React from 'react';

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


                    <div className="row">
                        <div className="col-12">
                            <div className="card border-primary">
                                <div className="card-body">
                                    <div className="card-text">

                                        <div className="row">
                                            <h5>Rule #1 - Role Play</h5>
                                            <br />
                                            <br />
                                            <p>
                                                You are expected to role play in all interactions with players and DMs. You may not use famous characters, unearned titles or character concepts which are harmful to the world (e.g. Luke Skywalker, Revan or Darth Vader). Character concepts may be rejected at a DM’s discretion and you are expected to remake without complaint.
                                            </p>
                                        </div>

                                        <div className="row">
                                            <h5>Rule #2 - Play Respectfully</h5>
                                            <br />
                                            <br />
                                            <p>
                                                Cybering and erotic role play (ERP) is NOT PERMITTED on this server. Our doors are open to a vast number of age ranges and, for this reason, restrict you to a “PG-13” level of interaction. Unwarranted rudeness, potentially offensive role play, inappropriate sexual references, harassment, exploiting known or unknown bugs, logging to avoid consequences, etc are prohibited.
                                            </p>
                                        </div>

                                        <div className="row">
                                            <h5>Rule #3 - Listen to the DMs</h5>
                                            <br />
                                            <br />
                                            <p>
                                                Dungeon Masters are to be considered the final authority in any dispute, question, or issue that comes up. By playing Star Wars: Legends of the Old Republic you agree to abide by their decisions. If there is a dispute with a DM ruling or you feel you’ve been dealt with unfairly, OBEY THE RULING at the time and then contact the admin staff. You may reach us through Discord or in a private Message on the forums.
                                            </p>
                                        </div>

                                        <div className="row">
                                            <h5>Rule #4 - PvP</h5>
                                            <br />
                                            <br />
                                            <p>
                                                Combat actions against other PCs (PvP) must be interactively role played. This means: you interact, they interact, BEFORE any battle occurs. You are expected to wait one real-world day before participating in PvP or interacting in any way with that PC or other hostile PCs from the battle unless both sides explicitly agree otherwise. Having an opposing character type (i.e Jedi vs Sith) is not a sufficient reason for a PvP action.
                                            </p>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        );
    }
}
