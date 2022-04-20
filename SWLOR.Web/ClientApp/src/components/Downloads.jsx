import * as React from 'react';

export default class Downloads extends React.Component {
    constructor(props) {
        super(props);

        this.buildURL = this.buildURL.bind(this);
    }

    buildURL(id) {
        return '/Download/Index/' + id;
    }


    render() {
        return (
            <div>

                <div>
                    <div className="row">
                        <div className="col-12">
                            <div className="card border-primary mb-3 center">
                                <div className="card-body">
                                    <h4 className="card-title">Downloads</h4>

                                    <p>
                                        The main files necessary to play are distributed over NWSync. This means you can just join the server and get the files!
                                         <br />
                                        However, there are a few files which aren't distributed this way. You will need to manually install these if you would like them.
                                     </p>
                                </div>
                            </div>
                        </div>
                    </div>


                    <table className="table">
                        <thead>
                            <tr>
                                <th>File Name</th>
                                <th>Description</th>
                                <th>Instructions</th>
                                <th>Download</th>
                            </tr>
                        </thead>
                        <tbody>

                            <tr>
                                <td>GUI Override</td>
                                <td>This overrides the graphics of your user interface. It will affect all servers do you will need to move it out of your override folder if you no longer wish to use it.</td>
                                <td>Extract all files to your My Documents/Neverwinter Nights/override directory.</td>
                                <td>
                                    <a className="btn btn-primary btn-block" href={this.buildURL(1)} target="_blank" rel="noopener noreferrer">Download</a>
                                </td>
                            </tr>

                            <tr>
                                <td>SWLOR Haks</td>
                                <td>These are the DEVELOPMENT-ONLY hakpaks. If you only want to play, you DO NOT need these. Simply connect to the server to get the files and start playing.</td>
                                <td>Extract all .hak files to your My Documents/Neverwinter Nights/hak directory. Extract the swlor_tlk.tlk file to your tlk directory. Make the tlk directory if it doesn't already exist</td>
                                <td>
                                    <a className="btn btn-primary btn-block" href={this.buildURL(2)} target="_blank" rel="noopener noreferrer">Download</a>
                                </td>
                            </tr>

                            <tr>
                                <td>Android Texture Override</td>
                                <td>These are texture overrides for the Android version of the game to correct issues on this specific platform.</td>
                                <td>Extract these to your override folder of NWN.</td>
                                <td>
                                    <a className="btn btn-primary btn-block" href={this.buildURL(3)} target="_blank" rel="noopener noreferrer">Download</a>
                                </td>
                            </tr>
                        </tbody>

                    </table>
                </div>
            </div>
        );
    }
}
