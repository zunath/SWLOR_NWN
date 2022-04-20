import * as React from 'react';

export default class About extends React.Component {
    render() {
        return (
            <div>

                <h3 className="center">Welcome to Star Wars: Legends of the Old Republic!</h3>
                <h5 className="center">A Neverwinter Nights: Enhanced Edition Server</h5>

                <div className="row">
                    <div className="col">
                        <div className="card border-primary mb-3">
                            <div className="card-header">About Star Wars: LOR</div>
                            <div className="card-body">
                                <div className="card-text">
                                    Star Wars: LOR is a persistent world entirely driven by players. We give you the tools - you make the world!
                                    <br />
                                    <ul>
                                        <li>Skill-based progression</li>
                                        <li>Custom perks & force ability system</li>
                                        <li>Unique Star Wars setting</li>
                                        <li>Expansive sandbox</li>
                                        <li>Extensive crafting system</li>
                                        <li>Player-built bases</li>
                                        <li>And much more!</li>
                                    </ul>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div className="col">
                        <div className="card border-primary mb-3">
                            <div className="card-header">Connection Info</div>
                            <div className="card-body">
                                <div className="card-text">
                                    Direct Connect: play.starwarsnwn.com:5121
                                    <br />
                                    Discord: <a href="https://discord.gg/MyQAM6m" target="_blank" rel="noopener noreferrer">Click Here</a>
                                </div>
                            </div>
                        </div>

                        <div className="card border-primary mb-3">
                            <div className="card-header">Buy Neverwinter Nights: Enhanced Edition</div>
                            <div className="card-body">
                                <div className="card-text">
                                    Buy on Steam: <a href="http://store.steampowered.com/app/704450/Neverwinter_Nights_Enhanced_Edition/" target="_blank" rel="noopener noreferrer">Click Here</a>
                                    <br />
                                    Buy from Beamdog: <a href="https://www.beamdog.com/products/neverwinter-nights-enhanced-edition#signup-form" target="_blank" rel="noopener noreferrer">Click Here</a>
                                    <br />
                                    Buy from GOG: <a href="https://www.gog.com/game/neverwinter_nights_enhanced_edition_pack" target="_blank" rel="noopener noreferrer">Click Here</a>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>


                
            </div>
        );



    }
}
